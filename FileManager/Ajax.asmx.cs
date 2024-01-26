using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;

namespace FileManager
{
    /// <summary>
    /// Summary description for Ajax
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Ajax : System.Web.Services.WebService
    {

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public void HelloWorld(string x)
        {
            var y = new { A = x , B=x+1};
            ResponseJson(y);
        }


        private void ResponseJson(object obj)
        {
			Context.Response.Clear();
			Context.Response.ContentType = "application/json";
			Context.Response.Write(JsonConvert.SerializeObject(obj));
		}
    }
}
