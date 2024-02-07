using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;

namespace FileManager
{
	[WebService(Namespace = "http://tempuri.org/")]
	[ScriptService]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	// [System.Web.Script.Services.ScriptService]
	public class FileManagerHandler : System.Web.Services.WebService
	{
		// Thư mục upload file
		private const string UPLOAD_ROOT_DIR = "/upload";
		// Thư mục chứa ảnh xem trước
		// Dùng cho chức năng tạo thumbnail
		private const string THUMB_DIR = ".tmb";
		// Ảnh mặc định của thư mục
		private const string DEFAULT_DIR_ICON = "/assets/libs/filemanager/icon/folder-solid.svg";
		// Ảnh mặc định của các loại file
		private const string DEFAULT_FILE_ICON = "/assets/libs/filemanager/icon/file-alt-solid.svg";
		private const string DEFAULT_ARCHIVE_ICON = "/assets/libs/filemanager/icon/file-archive-solid.svg";
		private const string DEFAULT_AUDIO_ICON = "/assets/libs/filemanager/icon/file-audio-solid.svg";
		private const string DEFAULT_EXCEL_ICON = "/assets/libs/filemanager/icon/file-excel-solid.svg";
		private const string DEFAULT_IMAGE_ICON = "/assets/libs/filemanager/icon/file-image-solid.svg";
		private const string DEFAULT_PDF_ICON = "/assets/libs/filemanager/icon/file-pdf-solid.svg";
		private const string DEFAULT_POWERPOINT_ICON = "/assets/libs/filemanager/icon/file-powerpoint-solid.svg";
		private const string DEFAULT_VIDEO_ICON = "/assets/libs/filemanager/icon/file-video-solid.svg";
		private const string DEFAULT_WORD_ICON = "/assets/libs/filemanager/icon/file-word-solid.svg";

		// Kích thước file upload tối đa, tính bằng BYTE
		// 10MB
		private const long MAX_FILE_SIZE_IN_BYTE = 10 * 1024 * 1024;
		// Kích thước ảnh preview, tính bằng px
		private const int THUMBNAIL_MAX_SIZE = 80;

		private const string ERR_DIR_OR_FILE_NOT_FOUND = "Không tìm thấy tệp hoặc thư mục.";
		private const string ERR_DIR_OR_FILE_EXISTS = "Tệp hoặc thư mục đã tồn tại.";
		private const string ERR_DIR_NOT_FOUND = "Không tìm thấy thư mục.";
		private const string ERR_DIR_EXISTS = "Thư mục đã tồn tại.";
		private const string ERR_FILE_NOT_FOUND = "Không tìm thấy tệp.";
		private const string ERR_BAD_REUQEST = "Dữ liệu không hợp lệ.";
		private const string ERR_INTERNAL_SERVER = "Đã xảy ra lỗi trong quá trình xử lý yêu cầu (500). ";

		private string RootPath
		{
			get
			{
				return Server.MapPath(UPLOAD_ROOT_DIR.TrimStart("\\/".ToCharArray()));
			}
		}

		private readonly Dictionary<string, string[]> _fileExtMapper = new Dictionary<string, string[]>();
		public FileManagerHandler()
		{
			_fileExtMapper.Add(DEFAULT_ARCHIVE_ICON, new string[] { ".rar", ".zip", ".7z" });
			_fileExtMapper.Add(DEFAULT_AUDIO_ICON, new string[] { ".mp3" });
			_fileExtMapper.Add(DEFAULT_EXCEL_ICON, new string[] { ".xls", ".xlsx", ".csv" });
			_fileExtMapper.Add(DEFAULT_IMAGE_ICON, new string[] { ".jpeg", ".jpg", ".png", ".gif", ".webp", ".tiff" });
			_fileExtMapper.Add(DEFAULT_PDF_ICON, new string[] { ".pdf" });
			_fileExtMapper.Add(DEFAULT_POWERPOINT_ICON, new string[] { ".ppt", ".pptx" });
			_fileExtMapper.Add(DEFAULT_VIDEO_ICON, new string[] { ".mp4" });
			_fileExtMapper.Add(DEFAULT_WORD_ICON, new string[] { ".doc", ".docx" });
		}

		/// <summary>
		/// Upload file
		/// </summary>
		/// <param name="dir">Thư mục chứa file upload</param>
		/// <returns>true nếu upload thành công</returns>
		[WebMethod]
		[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
		public CommonResponseModel Upload(string dir, string[] fileNames, string[] base64Values)
		{
			dir = StandardizeDir(dir);
			var errMesg = "";
			var fullDirPath = Path.Combine(RootPath, dir);
			if (fileNames.Length != base64Values.Length)
			{
				return new CommonResponseModel
				{
					StatusCode = HttpStatusCode.BadRequest,
					Message = ERR_BAD_REUQEST
				};
			}

			if (!Directory.Exists(fullDirPath))
			{
				return new CommonResponseModel
				{
					StatusCode = HttpStatusCode.BadRequest,
					Message = ERR_DIR_NOT_FOUND
				};
			}
			for (int i = 0; i < fileNames.Length; i++)
			{
				var fullFilePath = Path.Combine(fullDirPath, fileNames[i]);

				// Nếu trùng tên thì thêm _2 vào tên file
				while (File.Exists(fullFilePath))
				{
					var fileWithoutExt = Path.GetFileNameWithoutExtension(fullFilePath);
					var fileExt = Path.GetExtension(fullFilePath);
					var folderPath = Path.GetDirectoryName(fullFilePath);
					fullFilePath = Path.Combine(folderPath, fileWithoutExt + "_2" + fileExt);
				}

				var bytes = Convert.FromBase64String(base64Values[i]);
				if (bytes.LongLength <= MAX_FILE_SIZE_IN_BYTE)
				{
					File.WriteAllBytes(fullFilePath, bytes);
				}
				else
				{
					errMesg += string.Format("Tệp [{0} ({1:N2}MB)] vượt quá kích thước tối đa của hệ thống ({2:N2}MB)\n",
								fileNames[i],
								(double)bytes.LongLength / 1024 / 1024,
								(double)MAX_FILE_SIZE_IN_BYTE / 1024 / 1024);
				}
			}

			if (!string.IsNullOrEmpty(errMesg))
			{
				return new CommonResponseModel
				{
					StatusCode = HttpStatusCode.BadRequest,
					Message = errMesg
				};
			}
			return new CommonResponseModel
			{
				StatusCode = HttpStatusCode.OK,
				Message = "Tải lên thành công " + fileNames.Length + " tệp."
			};
		}

		private void GetDirectories(string subDir = "")
		{
			try
			{
				// Chuẩn hóa param
				subDir = StandardizeDir(subDir);
				var fullPath = Path.Combine(RootPath, subDir);
				if (!Directory.Exists(fullPath))
				{
					ResponseErrorJson(HttpStatusCode.BadRequest, ERR_DIR_NOT_FOUND);
					return;
				}
				var dirs = Directory.GetDirectories(fullPath)
									.Select(d => d.Replace(RootPath + "\\", ""));
				ResponseSuccessJson(dirs);
			}
			catch (Exception ex)
			{
				ResponseErrorJson(HttpStatusCode.InternalServerError, ERR_INTERNAL_SERVER + ex.Message);
			}
		}
		private void GetDefaultDirectories(string subDir)
		{
			try
			{
				// Chuẩn hóa param
				subDir = subDir.Replace("/", "\\");
				subDir = StandardizeDir(subDir);
				var fullPath = Path.Combine(RootPath, subDir);
				if (!Directory.Exists(fullPath))
				{
					ResponseErrorJson(HttpStatusCode.BadRequest, ERR_DIR_NOT_FOUND);
					return;
				}

				// Cộng dồn thư mục, tính từ thư mục gốc cho đến thư mục mặc định
				var folderSegments = subDir.Split('\\');
				List<string> paths = new List<string>();
				fullPath = RootPath;

				int i = 0;
				do
				{
					var dirs = Directory.GetDirectories(fullPath)
									.Select(d => d.Replace(RootPath + "\\", ""));
					paths.AddRange(dirs);
					if (i < folderSegments.Length)
					{
						fullPath = Path.Combine(fullPath, folderSegments[i]);
					}
					i++;
				} while (i <= folderSegments.Length);
				ResponseSuccessJson(paths);
			}
			catch (Exception ex)
			{
				ResponseErrorJson(HttpStatusCode.InternalServerError, ERR_INTERNAL_SERVER + ex.Message);
			}
		}

		private void GetFilesAndFoldersInDir(string subDir = "")
		{
			try
			{
				// Chuẩn hóa param
				subDir = StandardizeDir(subDir);
				var fullPath = Path.Combine(RootPath, subDir);
				if (!Directory.Exists(fullPath))
				{
					ResponseErrorJson(HttpStatusCode.BadRequest, ERR_FILE_NOT_FOUND);
					return;
				}

				var files = Directory.GetFiles(fullPath)
							.Select(d => new FileAndFolderModel
							{
								IsFolder = false,
								Path = d.Replace(RootPath + "\\", "")
							}).ToList();
				CreateThumbForFiles(files);

				var folders = Directory.GetDirectories(fullPath)
							.Select(d => new FileAndFolderModel
							{
								IsFolder = true,
								Path = d.Replace(RootPath + "\\", ""),
								ThumbPath = DEFAULT_DIR_ICON
							}).ToList();
				folders.AddRange(files);
				ResponseSuccessJson(folders);
			}
			catch (Exception ex)
			{
				ResponseErrorJson(HttpStatusCode.InternalServerError, ERR_INTERNAL_SERVER + ex.Message);
			}
		}

		[WebMethod]
		[ScriptMethod]
		public void ExecuteCommand(string command, string value1, string value2)
		{
			switch (command)
			{
				case "get_setting":
					{
						GetSetting();
						break;
					}
				case "rename":
					{
						Rename(value1, value2);
						break;
					}
				case "create_folder":
					{
						CreateFolder(value1);
						break;
					}
				case "select_default":
					{
						GetDefaultDirectories(value1);
						break;
					}
				case "select_dir":
					{
						GetDirectories(value1);
						break;
					}
				case "select_all":
					{
						GetFilesAndFoldersInDir(value1);
						break;
					}
				case "del":
					{
						DelFileOrFolder(value1);
						break;
					}
				default:
					{

						break;
					}
			}
		}

		private void GetSetting()
		{
			ResponseSuccessJson(new
			{
				rootPath = UPLOAD_ROOT_DIR,
				maxFileSizeAllow = MAX_FILE_SIZE_IN_BYTE,
				thumbPath = UPLOAD_ROOT_DIR + "/" + THUMB_DIR,
			});
		}

		/// <summary>
		/// Xóa file hoặc thư mục theo đường dẫn chỉ định
		/// </summary>
		private void DelFileOrFolder(string path)
		{
			path = StandardizeDir(path);
			var fullPath = Path.Combine(RootPath, path);

			if (Directory.Exists(fullPath))
			{
				// Xóa thư mục và thư mục con
				Directory.Delete(fullPath, true);
			}
			else if (File.Exists(fullPath))
			{
				// Xóa file
				File.Delete(fullPath);
			}
		}

		private void CreateFolder(string path)
		{
			path = StandardizeDir(path);
			var fullPath = Path.Combine(RootPath, path);
			if (Directory.Exists(fullPath))
			{
				ResponseErrorJson(HttpStatusCode.BadRequest, ERR_DIR_EXISTS);
				return;
			}
			Directory.CreateDirectory(fullPath);
			ResponseSuccessJson(null);
		}

		void Rename(string oldPath, string newName)
		{
			oldPath = StandardizeDir(oldPath);
			newName = StandardizeDir(newName);
			oldPath = Path.Combine(RootPath, oldPath);
			// Lỗi nếu không tồn tại thư mục/file
			if (!(Directory.Exists(oldPath) || File.Exists(oldPath)))
			{
				ResponseErrorJson(HttpStatusCode.BadRequest, ERR_DIR_OR_FILE_NOT_FOUND);
				return;
			}
			var newPath = Directory.GetParent(oldPath).FullName;
			newPath = Path.Combine(newPath, newName);
			// Lỗi nếu tên sau khi rename đã tồn tại
			if (Directory.Exists(newPath) || File.Exists(newPath))
			{
				ResponseErrorJson(HttpStatusCode.BadRequest, ERR_DIR_OR_FILE_EXISTS);
				return;
			}
			try
			{
				// Kiểm tra xem path cũ là thư mục hay file
				// Dựa vào đó để dùng hàm đổi tên phù hợp
				FileAttributes attr = File.GetAttributes(oldPath);
				if (attr.HasFlag(FileAttributes.Directory))
				{
					Directory.Move(oldPath, newPath);
					ResponseSuccessJson(null, "Đổi tên thư mục thành công");
				}
				else
				{
					File.Move(oldPath, newPath);
					ResponseSuccessJson(null, "Đổi tên tệp thành công");
				}
			}
			catch (Exception ex)
			{
				ResponseErrorJson(HttpStatusCode.BadRequest, ERR_BAD_REUQEST + " " + ex.Message);
			}
		}

		/// <summary>
		/// Tiêu chuẩn hóa param để tránh lỗi
		/// </summary>
		private string StandardizeDir(string dir)
		{
			return dir.Replace("'", "")
					  .Replace("*", "")
					  .Replace("?", "")
					  .Replace("@", "")
					  .Replace("..", "")       // Xóa 2 dấu chấm liên tiếp vì lý do bảo mật
					  .Trim(new char[] { '/', '\\', ' ' });
		}

		private void ResponseSuccessJson(object obj, string message = "")
		{
			var objResponse = new
			{
				StatusCode = (int)HttpStatusCode.OK,
				Message = message,
				Data = obj
			};
			var json = JsonConvert.SerializeObject(objResponse);
			Context.Response.Clear();
			Context.Response.BufferOutput = true;
			Context.Response.ContentType = "application/json; charset=utf-8";
			Context.Response.StatusCode = (int)HttpStatusCode.OK;
			Context.Response.Write(json);
		}
		private void ResponseErrorJson(HttpStatusCode statusCode, string errMessage)
		{
			var objResponse = new
			{
				StatusCode = (int)statusCode,
				Message = errMessage
			};
			Context.Response.Clear();
			Context.Response.BufferOutput = true;
			Context.Response.ContentType = "application/json";
			Context.Response.StatusCode = (int)statusCode;
			Context.Response.Write(JsonConvert.SerializeObject(objResponse));
		}

		private void CreateThumbForFiles(List<FileAndFolderModel> files)
		{
			for (int i = 0; i < files.Count; i++)
			{
				var fileExt = Path.GetExtension(files[i].Path).ToLower();
				foreach (var m in _fileExtMapper)
				{
					if (m.Value.Contains(fileExt))
					{
						if (m.Key == DEFAULT_IMAGE_ICON)
						{
							var thumbDir = Path.Combine(RootPath, THUMB_DIR);
							var thumbName = CreateMD5(Path.Combine(RootPath, files[i].Path)) + fileExt;
							var thumbFullPath = Path.Combine(thumbDir, thumbName);
							if (File.Exists(thumbFullPath))
							{
								files[i].ThumbPath = UPLOAD_ROOT_DIR + thumbFullPath.Replace(RootPath, "");
							}
							else
							{
								var thumbPath = CreateThumb(Path.Combine(RootPath, files[i].Path), thumbDir);
								if (String.IsNullOrEmpty(thumbPath))
								{
									files[i].ThumbPath = DEFAULT_IMAGE_ICON;
								}
								else
								{
									files[i].ThumbPath = UPLOAD_ROOT_DIR + thumbPath.Replace(RootPath, "");
								}
							}
						}
						else
						{
							files[i].ThumbPath = m.Key;
						}
						break;
					}
				}
				if (string.IsNullOrEmpty(files[i].ThumbPath))
				{
					files[i].ThumbPath = DEFAULT_FILE_ICON;
				}
			}
		}

		/// <param name="imgPath"></param>
		/// <param name="thumbDir"></param>
		/// <returns>Đường dẫn của file thumbnail</returns>
		string CreateThumb(string imgPath, string thumbDir)
		{
			try
			{
				var image = Image.FromFile(imgPath);
				int thumbW = 0, thumbH = 0;
				var thumbName = CreateMD5(imgPath);
				var thumbExt = Path.GetExtension(imgPath);
				if (image.Width > image.Height)
				{
					thumbW = THUMBNAIL_MAX_SIZE;
					thumbH = image.Height * thumbW / image.Width;
				}
				else
				{
					thumbH = THUMBNAIL_MAX_SIZE;
					thumbW = image.Width * thumbH / image.Height;
				}

				var destRect = new Rectangle(0, 0, thumbW, thumbH);
				var destImage = new Bitmap(thumbW, thumbH);

				using (var graphics = Graphics.FromImage(destImage))
				{
					graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
				}
				var path = Path.Combine(thumbDir, thumbName + thumbExt);
				destImage.Save(path);
				destImage.Dispose();
				image.Dispose();
				return path;
			}
			catch (Exception ex)
			{
				var filename = Server.MapPath("FILE_MANAGER_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
				var content = DateTime.Now.ToString() + ": " + ex.Message
								+ Environment.NewLine
								+ Environment.NewLine
								+ ex.StackTrace;
				File.AppendAllText(filename, content);
				return "";
			}
		}

		string CreateMD5(string input)
		{
			// Use input string to calculate MD5 hash
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
				byte[] hashBytes = md5.ComputeHash(inputBytes);

				StringBuilder sb = new System.Text.StringBuilder();
				for (int i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("x2"));
				}
				return sb.ToString();
			}
		}
	}

	public class CommonResponseModel
	{
		public HttpStatusCode StatusCode { get; set; }
		public string Message { get; set; }
		public object Data { get; set; }
	}

	public class FileAndFolderModel
	{
		public bool IsFolder { get; set; }
		public string ThumbPath { get; set; }
		public string Path { get; set; }
		public FileAndFolderModel()
		{
			IsFolder = false;
		}
	}
}