<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="FileManager.Index" %>
<%@ Register Src="~/UCFileManager/FileManager.ascx" TagPrefix="uc1" TagName="FileManager" %>


<!DOCTYPE html>

    <html xmlns="http://www.w3.org/1999/xhtml">

    <head runat="server">
        <title></title>
        <link href="~/assets/libs/filemanager/filemanager.css" rel="stylesheet" />
    </head>

    <body>
        <form id="form1" runat="server">
            <uc1:FileManager runat="server" id="FileManager" />

            <div style="margin: 50px">
                <h2>[Test] Nhận giá trị được chọn từ filemanager</h2>
                <button id="btnGetResult">Nhấn để nhận</button>
                <p id="result"></p>
            </div>
        </form>

        <script src="assets/libs/jquery/jquery-3.6.0.min.js"></script>
        <script defer src="assets/libs/alpinejs/alpinejs-3.13.5.min.js"></script>
        <script src="assets/libs/filemanager/filemanager.js"></script>
    </body>

    </html>