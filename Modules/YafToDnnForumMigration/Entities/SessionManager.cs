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