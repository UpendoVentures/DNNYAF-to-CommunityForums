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