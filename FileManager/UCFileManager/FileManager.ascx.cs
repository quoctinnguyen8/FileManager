using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace FileManager.UCFileManager
{
    public partial class FileManager : System.Web.UI.UserControl
    {
        // TODO: Cho phép nhiều component trong 1 trang, phân biệt nhau bằng tên
        public string Name { get; set; }
        public string AjaxPath { get; set; } 
        public bool IsPopup { get; set; } 
        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}