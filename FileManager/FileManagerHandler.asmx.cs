using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
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
		private const string ROOT_DIR = "/upload";
		// Thư mục chứa ảnh xem trước
		// TODO: dùng cho chức năng tạo thumbnail
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
		private const long MAX_FILE_SIZE_IN_BYTE = 5 * 1024 * 1024;  // 5MB
		private const string ERR_DIR_NOT_FOUND = "Không tìm thấy thư mục.";
		private const string ERR_DIR_EXISTS = "Thư mục đã tồn tại.";
		private const string ERR_FILE_NOT_FOUND = "Không tìm thấy tệp.";
		private const string ERR_BAD_REUQEST = "Dữ liệu không hợp lệ.";
		private const string ERR_INTERNAL_SERVER = "Đã xảy ra lỗi trong quá trình xử lý yêu cầu (500). ";

		private string RootPath { get { return Server.MapPath(ROOT_DIR); } }

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
				// TODO: Tạo file thumbnail nếu không tồn tại

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
		public void ExecuteCommand(string command, string value)
		{
			switch (command)
			{
				case "get_setting":
					{
						GetSetting();
						break;
					}
				case "create_folder":
					{
						CreateFolder(value);
						break;
					}
				case "select_dir":
					{
						GetDirectories(value);
						break;
					}
				case "select_all":
					{
						GetFilesAndFoldersInDir(value);
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
				rootPath = ROOT_DIR,
				maxFileSizeAllow = MAX_FILE_SIZE_IN_BYTE,
				thumbPath = ROOT_DIR + "/" + THUMB_DIR,
			});
		}

		/// <summary>
		/// Xóa file theo đường dẫn chỉ định
		/// </summary>
		/// <returns>true nếu xóa thành công</returns>
		private bool Del(string path)
		{
			path = StandardizeDir(path);
			var fullPath = Path.Combine(RootPath, path);

			// Xóa file
			if (File.Exists(fullPath))
			{
				File.Delete(fullPath);
				return true;
			}

			// TODO: xóa thư mục
			return false;
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
					  .TrimStart(new char[] { '/', '\\' });
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
						files[i].ThumbPath = m.Key;
						break;
					}
				}
				if (string.IsNullOrEmpty(files[i].ThumbPath))
				{
					files[i].ThumbPath = DEFAULT_FILE_ICON;
				}
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
