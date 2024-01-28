<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="FileManager.Index" %>

    <!DOCTYPE html>

    <html xmlns="http://www.w3.org/1999/xhtml">

    <head runat="server">
        <title></title>
        <link href="~/assets/libs/filemanager/filemanager.css" rel="stylesheet" />
    </head>

    <body>
        <form id="form1" runat="server">

            <div x-data="filemanager('/FileManagerHandler.asmx')" class="filemanager__container">
                <div class="filemanager__dir-tree">
                    <div class="dir-item" x-on:click="getDirsIn(null, -1)">
                    <img src="assets/libs/filemanager/icon/hdd-solid.svg" class="dir-icon" />
                    <span>Thư mục gốc</span>
                </div>
                    <template x-for="(d, idx) in _dirs">
                        <div class="dir-item"
                            :class="{['dir-item-level-' + d.level]: true, selected: idx == _dirSelectedIndex}"
                            x-on:click="getDirsIn(d, idx)">
                            <template x-if="idx == _dirSelectedIndex">
                                <img src="assets/libs/filemanager/icon/folder-open-solid.svg" class="dir-icon" />
                            </template>
                            <template x-if="idx != _dirSelectedIndex">
                                <img src="assets/libs/filemanager/icon/folder-solid.svg" class="dir-icon" />
                            </template>
                            <span x-text="d.name"></span>
                        </div>
                    </template>
                </div>

                <div class="filemanager__panel">
                    <div class="panel-toolbox">
                        <input type="file" x-ref="fileUpload" multiple />
                        <button x-on:click="uploadFile()" class="fbutton" type="button">Upload</button>
                    </div>
                    <div class="filemanager__panel-container">
                        <template x-for="(f, idx) in _filesAndFolders">
                            <div class="panel-item" :class="{selected: idx == _fileSelectedIndex}" :title="f.name" 
                                x-on:click="selectItemInPanel(f.fullPath, idx)" x-on:dblclick="openItem(f)">
                                <div class="panel-item__thumb">
                                    <img :src="f.thumb" />
                                </div>
                                <div class="panel-item__filename" x-text="f.name"></div>
                            </div>
                        </template>
                    </div>
                    <div class="panel-bottom">
                        Đường dẫn: <span x-text="_dirSelectedPath ? _dirSelectedPath : '/'"></span> |
                        Tệp đang chọn: <span x-text="_fileSelected"></span>
                    </div>
                </div>

            </div>
        </form>

        <script src="assets/libs/jquery/jquery-3.6.0.min.js"></script>
        <script defer src="assets/libs/alpinejs/alpinejs-3.13.5.min.js"></script>
        <script src="assets/libs/filemanager/filemanager.js"></script>
    </body>

    </html>