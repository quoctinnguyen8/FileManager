String.prototype.isImage = function () {
	var segments = this.split('.');
	if (segments.length <= 1) return false;
	var ext = segments.pop().toLowerCase();
	return ['png', 'jpeg', 'jpg', 'gif', 'webp'].indexOf(ext) >= 0;
}


document.addEventListener('alpine:init', () => {
	Alpine.data('filemanager', (name, fileManagerAjaxUrl, isPopup) => ({
		_url: {
			executeCmd: fileManagerAjaxUrl + '/ExecuteCommand',
			upload: fileManagerAjaxUrl + '/Upload'
		},
		_cmdData: {
			command: '',
			value: '',
		},
		_toolbox: {
			isShowNewFolder: false,
		},
		_popupProperty: {
			popup: !!isPopup,
			show: false,
			showCount: 0,
		},
		_setting: {
			rootPath: "/upload",	// Dùng để thêm vào trước đường dẫn file
			maxFileSizeAllow: 0,	// Kích thước file tối đa tính bằng byte
			thumbPath: "/.tmb",
			defaultFolder: '',
		},
		_loader: {
			show: false,
			message: '0.00%'
		},

		// Chức năng upload
		_uploadData: {
			dir: '',
			fileNames: [],
			base64Values: []
		},

		// Cây thư mục
		_dirs: [
			{
				level: 1,
				name: 'Đang tải...',
				fullPath: ''
			}
		],
		_dirSelectedIndex: -1,
		_dirSelectedPath: '',

		// Danh sách file trong thư mục, hiển thị ở panel (khu vực chính của filemanager)
		_filesAndFolders: [
			//{
			//	isFolder: false,
			//	name: 'Đang tải...',
			//	fullPath: ''
			//}
		],
		_fileSelectedIndex: -1,
		_fileSelected: '',
		// callback cho sự kiện chọn file ở mode popup
		_onFileSelectedCallback: null,

		init() {
			this._dirs = [];
			this.addCustomEvent();
			this.$watch('_fileSelectedIndex', (newVal, oldVal) => {
				if (newVal >= 0) {
					window[`filemanager.${name}.selectedValue`] = this._filesAndFolders[newVal].fullPath.replace(/\\/g, '/');
				} else {
					window[`filemanager.${name}.selectedValue`] = '';
				}
			});
			this.getSetting();
			// Hiển thị thư mục ngay nếu không phải là popup
			// Nếu là popup thì chờ kích hoạt event mới hiện
			if (!this._popupProperty.popup) {
				this.reloadPanel();	
			}
		},
		getSetting() {
			this._cmdData.command = "get_setting";
			this._cmdData.value = '';
			$.get(this._url.executeCmd, this._cmdData)
				.then((res) => {
					this._setting.rootPath = res.Data.rootPath;
					this._setting.maxFileSizeAllow = res.Data.maxFileSizeAllow;
					this._setting.thumbPath = res.Data.thumbPath;
				});
		},

		getDirsIn(fullDirPath, idx) {
			this._cmdData.command = "select_dir";
			this._cmdData.value = fullDirPath;
			this._dirSelectedIndex = idx;
			this._dirSelectedPath = fullDirPath;
			this._fileSelected = '';

			$.ajax({
				type: "GET",
				url: this._url.executeCmd,
				data: this._cmdData,
				success: (res) => {
					if (res.Data && res.Data.length) {
						var tmpDirs = [];
						for (var i = 0; i < res.Data.length; i++) {
							const obj = res.Data[i];
							const segments = obj.split("\\");
							var level = segments.length;
							var name = segments.pop();

							if (obj == ".tmb") continue;

							tmpDirs.push({
								level: level,
								name: name,
								fullPath: obj
							});
						}
						this.addDirsToTree(tmpDirs, idx);
					}
				}
			});
			setTimeout(() => {
				this.getFilesAndFoldersIn(fullDirPath);
			}, 100);
		},

		addDirsToTree(dirs, idx) {
			var dirsBefore = this._dirs.slice(0, idx + 1);
			for (var i = idx + 1; i < idx + 1 + dirs.length; i++) {
				if (!this._dirs.some(d => d.fullPath == dirs[i - idx - 1].fullPath)) {
					dirsBefore.push(dirs[i - idx - 1]);
				}
			}
			this._dirs = dirsBefore.concat(this._dirs.slice(idx + 1));
		},

		// Nhận item và hiển thị ở panel
		getFilesAndFoldersIn(dirName) {
			this._cmdData.command = "select_all";
			this._cmdData.value = dirName;
			this._fileSelectedIndex = -1;

			$.ajax({
				type: "GET",
				url: this._url.executeCmd,
				data: this._cmdData,
				success: (res) => {
					if (res.Data) {
						var tmpFiles = [];
						for (var i = 0; i < res.Data.length; i++) {
							const obj = res.Data[i];
							const segments = obj.Path.split("\\");
							var name = segments.pop();

							if (obj.Path == ".tmb") continue;

							tmpFiles.push({
								isFolder: obj.IsFolder,
								name: name,
								fullPath: obj.Path,
								thumb: obj.ThumbPath
							});
						}
						this._filesAndFolders = tmpFiles;
					}
				}
			});
		},

		// Sự kiện khi click vào 1 item trên panel
		selectItemInPanel(fullPath, idx) {
			// Bỏ chọn nếu chọn lại file đang chọn
			if (this._fileSelectedIndex == idx) {
				this._fileSelected = '';
				this._fileSelectedIndex = -1;
			} else {
				this._fileSelectedIndex = idx;
				this._fileSelected = this._filesAndFolders[idx].name;
			}
		},

		// Sự kiện khi double-click vào 1 item trên panel
		openFolderOrSelectFile(itemOnPanel) {
			if (itemOnPanel.isFolder) {
				var idx = this._dirs.findIndex(d => d.fullPath == itemOnPanel.fullPath);
				this.getDirsIn(itemOnPanel.fullPath, idx);
				return;
			}

			// double click lên file
			// ở mode popup thì gọi callback
			if (this._onFileSelectedCallback) {
				var fullFileName = this._setting.rootPath + "/" + itemOnPanel.fullPath.replace(/\\/g, '/');
				this._onFileSelectedCallback(fullFileName);
			}
		},
		reloadPanel() {
			var idx = -1;
			var fullPath = '';
			if (this._dirSelectedIndex >= 0) {
				idx = this._dirSelectedIndex;
				var dir = this._dirs[this._dirSelectedIndex];
				fullPath = dir.fullPath;
			}
			this.getDirsIn(fullPath, idx);
		},
		downloadSeletecItem() {
			if (this._fileSelectedIndex < 0) return;
			var selectedItem = this._filesAndFolders[this._fileSelectedIndex];

			if (selectedItem.isFolder) return;

			var link = document.createElement("a");
			link.download = selectedItem.name;
			link.href = this._setting.rootPath + "/" + selectedItem.fullPath.replace(/\\/g, '/');
			document.body.appendChild(link);
			link.click();
			document.body.removeChild(link);
			delete link;
		},
		deleteSeletecItem() {
			if (this._fileSelectedIndex < 0) return;
			var selectedItem = this._filesAndFolders[this._fileSelectedIndex];
			var mesg;
			if (selectedItem.isFolder) {
				mesg = `Thư mục con và các tập tin bên trong cũng sẽ bị xóa. Xác nhận xóa thư mục ${selectedItem.name}?`;
			} else {
				mesg = `Xác nhận xóa tập tin ${selectedItem.name}?`;
			}
			if (confirm(mesg)) {
				this._cmdData.command = "del";
				this._cmdData.value = selectedItem.fullPath;
				$.get(this._url.executeCmd, this._cmdData)
					.then((res) => {
						if (selectedItem.isFolder) {
							this._dirs = this._dirs.filter(d => d.fullPath.indexOf(selectedItem.fullPath) != 0);
						}
						this.reloadPanel();
					});
			}
		},

		// Sự kiện khi nhấn nút 'Upload' trên toolbox
		// Do hạn chế về thời gian và công nghệ, hiện tại đang cho upload file dưới dạng base64
		async uploadFile() {
			if (this._dirSelectedIndex >= 0) {
				this._uploadData.dir = this._dirs[this._dirSelectedIndex].fullPath;
			} else {
				this._uploadData.dir = '';
			}
			this._uploadData.fileNames = [];
			this._uploadData.base64Values = [];
			var files = this.$refs['fileUpload_' + name].files;
			if (!files || files.length == 0) {
				alert('Chưa chọn file');
				return;
			}

			// Kiểm tra dung lượng file
			for (var i = 0; i < files.length; i++) {
				if (files[i].size == 0) {
					alert(`Tệp ${files[i].name} không hợp lệ.`);
					return;
				}
				if (files[i].size > this._setting.maxFileSizeAllow) {
					var maxSizeInMB = parseFloat(this._setting.maxFileSizeAllow / 1024 / 1024).toFixed(2);
					alert(`Tệp ${files[i].name} vượt quá kích thước tối đa. Tối đa ${maxSizeInMB}MB.`);
					return;
				}
				this._uploadData.fileNames.push(files[i].name);
				let base64 = await toBase64(files[i]);
				base64 = base64.split(",")[1];
				this._uploadData.base64Values.push(base64);
			}
			$.ajax({
				type: "POST",
				url: this._url.upload,
				async: true,
				data: JSON.stringify(this._uploadData),
				contentType: 'application/json',
				beforeSend: () => {
					this._loader.show = true;
					this._loader.message = '0.00%';
				},
				success: (data) => {
					this.reloadPanel();
				},
				error: function (error) {
					// handle error
				},
				complete: () => {
					this.$refs['fileUpload_' + name].value = "";
					this.changeLabelUpload();
					setTimeout(() => {
						this._loader.show = false;
					}, 500);
				},
				xhr: () => {
					// Hiển thị phần trăm upload
					var xhr = new window.XMLHttpRequest();
					xhr.upload.addEventListener("progress", (evt) => {
						if (evt.lengthComputable) {
							var percentComplete = evt.loaded / evt.total * 100;
							this._loader.message = percentComplete.toFixed(2) + '%';
						}
					}, false);
					return xhr;
				}
			});
		},
		createNewFolder() {
			var folderName = this.$refs['newFolderName_' + name].value;

			if (!folderName) {
				alert("Chưa nhập tên thư mục!");
				return;
			}
			this._cmdData.command = "create_folder";
			this._cmdData.value = this._dirSelectedPath + "\\" + folderName;
			this._fileSelectedIndex = -1;

			$.ajax({
				type: "GET",
				url: this._url.executeCmd,
				data: this._cmdData,
				success: (res) => {
					this.$refs['newFolderName_' + name].value = '';
					this.reloadPanel();
				},
				error: (err) => {
					alert(err.responseJSON.Message);
					this.$refs['newFolderName_' + name].focus();
				}
			});
		},

		// Sự kiện khi chọn file để upload
		changeLabelUpload() {
			var files = this.$refs['fileUpload_' + name].files;
			var lbl = this.$refs['labelFileUpload_' + name];
			if (files && files.length) {
				if (files.length == 1) {
					lbl.innerText = files[0].name;
					lbl.setAttribute("title", files[0].name);
				} else {
					lbl.innerText = "Đã chọn " + files.length + " tệp.";
					var title = "";
					for (var i = 0; i < files.length; i++) {
						title += files[i].name + "\n"
					}
					lbl.setAttribute("title", title);
				}
			} else {
				lbl.innerText = "Nhấn để chọn file";
				lbl.setAttribute("title", "");
			}
		},
		showFileManagerAsPopup() {
			if (!this._setting.defaultFolder || this._popupProperty.showCount > 0) {
				this.reloadPanel();
			}
			this._popupProperty.show = true;
			this._popupProperty.showCount++;
		},
		setDefaultFolder(defaultFolder) {
			// Xóa ký tự \,/, space ở đầu và cuối
			defaultFolder = defaultFolder.replace(/^(\s|\\|\/)+|(\\|\/|\s)+$/gm, '');
			if (this._popupProperty.showCount > 0) {
				return;
			}
			this._cmdData.command = "select_default";
			this._cmdData.value = defaultFolder;
			this._fileSelected = '';
			this._dirSelectedIndex = -1;
			this._setting.defaultFolder = defaultFolder;

			$.ajax({
				type: "GET",
				url: this._url.executeCmd,
				data: this._cmdData,
				success: (res) => {
					if (res.Data && res.Data.length) {
						var tmpDirs = [];
						for (var i = 0; i < res.Data.length; i++) {
							const obj = res.Data[i];
							const segments = obj.split("\\");
							var level = segments.length;
							var name = segments.pop();

							if (obj == ".tmb") continue;

							tmpDirs.push({
								level: level,
								name: name,
								fullPath: obj
							});
						}
						var tmpDirsSorted = tmpDirs.sort((a,b) => a.fullPath.localeCompare(b.fullPath));
						this._dirs = tmpDirsSorted;
						this._dirSelectedPath = defaultFolder.replace(/\//g, '\\');
						this._dirSelectedIndex = tmpDirsSorted.findIndex(d => d.fullPath == this._dirSelectedPath);
						this.getFilesAndFoldersIn(this._dirSelectedPath);
					}
				},
				error: () => {
					// Trường hợp lỗi thì quay về thư mục gốc
					this.reloadPanel();
				}
			});
		},
		addCustomEvent() {
			// sự kiện show file-manager
			var eventName = `filemanager.${name}.showAsPopup`;
			document.addEventListener(eventName, (e) => {
				this.showFileManagerAsPopup();
			});

			// sự kiện khi file được chọn
			eventName = `filemanager.${name}.onFileSelected`;
			document.addEventListener(eventName, (e) => {
				this._onFileSelectedCallback = e.detail.callback;
			});

			// sự kiện để đóng popup từ bên ngoài x-data
			eventName = `filemanager.${name}.close`;
			document.addEventListener(eventName, (e) => {
				this._popupProperty.show = false;
			});

			// sự kiện để set giá trị mặc định khi mở popup
			eventName = `filemanager.${name}.setDefaultFolder`;
			document.addEventListener(eventName, (e) => {
				var folder = e.detail.folder;
				this.setDefaultFolder(folder);
			});
		}
	}));
});

var FileManager = function (name, defaultFolder, style) {

	if (defaultFolder) {
		var eventName = `filemanager.${name}.setDefaultFolder`;
		var event = new CustomEvent(eventName, {
			detail: {
				folder: defaultFolder
			}
		});
		document.dispatchEvent(event);
	}

	this.showFileManagerAsPopup = function () {
		var eventName = `filemanager.${name}.showAsPopup`;
		var event = new CustomEvent(eventName);
		document.dispatchEvent(event);
	}

	this.closePopup = function () {
		var eventName = `filemanager.${name}.close`;
		var event = new CustomEvent(eventName);
		document.dispatchEvent(event);
	}
	this.onFileSelected = function (callback) {
		var eventName = `filemanager.${name}.onFileSelected`;
		var event = new CustomEvent(eventName, {
			detail: {
				callback: callback
			}
		});
		document.dispatchEvent(event);
	}
}

// Hàm convert file thành base64
const toBase64 = file => new Promise((resolve, reject) => {
	const reader = new FileReader();
	reader.readAsDataURL(file);
	reader.onload = () => resolve(reader.result);
	reader.onerror = reject;
});
