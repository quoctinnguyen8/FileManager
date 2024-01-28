document.addEventListener('alpine:init', () => {
    Alpine.data('filemanager', (fileManagerAjaxUrl) => ({
        _url: {
            executeCmd: fileManagerAjaxUrl + '/ExecuteCommand',
            upload: fileManagerAjaxUrl + '/Upload'
        },
        _cmdData: {
            command: '',
            value:'',
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

        // Danh sách file trong thư mục
        _filesAndFolders: [{
            isFolder: false,
            name: 'Đang tải...',
            fullPath: '' 
        }],
        _fileSelectedIndex: -1,
        _fileSelected: '',

        init() {
            this._cmdData.command = "select_dir";
            this._cmdData.value = "";
            $.ajax({
                url: this._url.executeCmd,
                type: "GET",
                data: this._cmdData,
                success: (res) => {
                    if (res.Data && res.Data.length) {
                        var tmpDirs = [];
                        for (var i = 0; i < res.Data.length; i++) {
                            const obj = res.Data[i];
                            if (obj != ".tmb") {
                                tmpDirs.push({
                                    level: 1,
                                    name: obj,
                                    fullPath: obj
                                });
                            }
                        }
                        this._dirs = tmpDirs;
                    }
                }
            });
        },

        getDirsIn(dirObj, idx) {
            this._cmdData.command = "select_dir";
            this._cmdData.value = dirObj ? dirObj.fullPath : '';
            this._dirSelectedIndex = idx;
            this._dirSelectedPath = dirObj ? dirObj.fullPath : '';

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
                this.getFilesAndFoldersIn(dirObj ? dirObj.fullPath : '');
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
        selectItemInPanel(fullPath, idx) {
            this._fileSelectedIndex = idx;
        },
        openItem(itemOnPanel) {
            if (itemOnPanel.isFolder) {
                var idx = this._dirs.findIndex(d => d.fullPath == itemOnPanel.fullPath);
                this.getDirsIn(this._dirs[idx], idx);
            }
        },
        reloadPanel() {
            // reload panel
            var idx = -1;
            var dir = null;
            if (this._dirSelectedIndex >= 0) {
                dir = this._dirs[this._dirSelectedIndex];
                idx = this._dirSelectedIndex;
            }
            this.getDirsIn(dir, idx);
        },
        async uploadFile() {
            if (this._dirSelectedIndex >= 0) {
                this._uploadData.dir = this._dirs[this._dirSelectedIndex].fullPath;
            } else {
                this._uploadData.dir = '';
            }
            this._uploadData.fileNames = [];
            this._uploadData.base64Values = [];
            var files = this.$refs.fileUpload.files;
            if (files) {
                for (var i = 0; i < files.length; i++) {
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
                    success: (data) => {
                        this.reloadPanel();
                    },
                    error: function (error) {
                        // handle error
                    },
                });
            }
        }
    }));
});

const toBase64 = file => new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => resolve(reader.result);
    reader.onerror = reject;
});
