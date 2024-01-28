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
        _uploadData: {
            dir: '',
            fileNames: [],
            base64Values: []
        },

        _dirs: [
            {
                level: 1,
                path: 'Đang tải...',
                fullPath: '',
                selected: false
            }
        ],

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
                            tmpDirs.push({
                                level: 1,
                                path: obj,
                                fullPath: obj,
                                selected: false
                            })
                        }
                        this._dirs = tmpDirs;
                    }
                }
            });
        },

        getDirsIn(dirName, idx) {
            this._cmdData.command = "select_dir";
            this._cmdData.value = dirName;
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
                            var path = segments.pop();

                            tmpDirs.push({
                                level: level,
                                path: path,
                                fullPath: obj,
                                selected: false
                            });
                        }
                        this.addDirsToTree(tmpDirs, idx);
                    }
                }
            })
        },

        addDirsToTree(dirs, idx) {
            var dirsBefore = this._dirs.slice(0, idx + 1);
            debugger;
            for (var i = idx + 1; i < idx + 1 + dirs.length; i++) {
                if (!this._dirs.some(d => d.fullPath == dirs[i - idx - 1].fullPath)) {
                    dirsBefore.push(dirs[i - idx - 1]);
                }
            }
            dirsBefore = dirsBefore.concat(this._dirs.slice(idx + 1));
			console.log(dirsBefore);
            this._dirs = dirsBefore;
        },

        async uploadFile() {
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
                    success: function (data) {
                        // your callback here
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
