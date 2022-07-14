using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using Upendo.Modules.YafToDnnForumMigration.Entities;

namespace Upendo.Modules.YafToDnnForumMigration
{
    /// <summary>
    /// Summary description for ImportProgress
    /// </summary>
    public class ImportProgress : IHttpHandler, IReadOnlySessionState
    {
        private const int MAXLENGTH = int.MaxValue;

        public void ProcessRequest(HttpContext context)
        {
            var res = HandleAction(context);
            context.Response.ContentType = "application/json";
            context.Response.Write(ObjectToJson(res));
        }

        private string ObjectToJson(object target)
        {
            var ser = new JavaScriptSerializer { MaxJsonLength = MAXLENGTH };
            return ser.Serialize(target);
        }

        protected object HandleAction(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated == false)
            {
                // not found
                context.Response.StatusCode = 404;
                context.Response.End();
                return null;
            }

            var method = context.Request.Params["method"];

            switch (method)
            {
                case "GetImportProgress":
                    return GetImportProgress(context);
                default:
                    break;
            }
            return true;
        }

        private object GetImportProgress(HttpContext context)
        {
            var manager = new SessionManager(context.Session);
            var returnObj = new object();
            lock (manager.AdminProductImportLog)
            {
                returnObj = new
                {
                    Progress = manager.AdminProductImportProgress,
                    Log = string.Join("\n", manager.AdminProductImportLog)
                };
            }
            return returnObj;
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}