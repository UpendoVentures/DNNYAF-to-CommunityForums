/*
MIT License

Copyright (c) Upendo Ventures, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
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