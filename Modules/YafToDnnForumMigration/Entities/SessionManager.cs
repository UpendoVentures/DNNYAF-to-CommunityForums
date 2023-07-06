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
using System.Web.SessionState;

namespace Upendo.Modules.YafToDnnForumMigration.Entities
{
    public class SessionManager
    {
        private const string _ImportProgress = "ForumImportProgress";
        private const string _ImportLog = "ForumImportLog";

        private readonly HttpSessionState _session;

        public SessionManager(HttpSessionState session)
        {
            _session = session;
        }

        public List<string> AdminProductImportLog
        {
            get { return (List<string>)GetSessionObjectInt(_ImportLog) ?? new List<string>(); }
            set { SetSessionObjectInt(_ImportLog, value); }
        }

        public double AdminProductImportProgress
        {
            get { return (double?)GetSessionObjectInt(_ImportProgress) ?? 0; }
            set { SetSessionObjectInt(_ImportProgress, value); }
        }

        private object GetSessionObjectInt(string variableName)
        {
            object result = null;

            if (_session[variableName] != null)
            {
                result = _session[variableName];
            }

            return result;
        }

        private void SetSessionObjectInt(string variableName, object value)
        {
            _session[variableName] = value;
        }

        public string GetSessionString(string variableName)
        {
            var result = string.Empty;

            var temp = GetSessionObject(variableName);
            if (temp != null)
            {
                result = (string)GetSessionObject(variableName);
            }

            return result;
        }

        public void SetSessionString(string variableName, string value)
        {
            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Session != null)
                {
                    HttpContext.Current.Session[variableName] = value ?? string.Empty;
                }
            }
        }

        public object GetSessionObject(string variableName)
        {
            object result = null;


            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Session != null)
                {
                    if (HttpContext.Current.Session[variableName] != null)
                    {
                        result = HttpContext.Current.Session[variableName];
                    }
                }
            }

            return result;
        }

        public void SetSessionObject(string variableName, object value)
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Session[variableName] = value;
            }
        }
    }
}