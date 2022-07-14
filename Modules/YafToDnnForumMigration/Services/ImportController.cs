using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using DotNetNuke.Security;
using DotNetNuke.Web.Api;
using Upendo.Modules.YafToDnnForumMigration.Entities;

namespace Upendo.Modules.YafToDnnForumMigration.Services
{
    [ValidateAntiForgeryToken]
    public class ImportController:DnnApiController
    {
        [DnnAuthorize]
        public HttpResponseMessage ImportStatus(HttpContext context)
        {
            try
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
                return Request.CreateResponse(HttpStatusCode.OK, returnObj);
            } catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex);
            }
        }
    }
}