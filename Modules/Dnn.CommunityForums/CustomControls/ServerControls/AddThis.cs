//
// Community Forums
// Copyright (c) 2013-2021
// by DNN Community
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DotNetNuke.Modules.ActiveForums.Controls
{
	[ToolboxData("<{0}:AddThis runat=server></{0}:AddThis>")]
	public class AddThis : WebControl
	{
		private string _addThisId;
		private string _title;
		public string AddThisId
		{
			get
			{
				return _addThisId;
			}
			set
			{
				_addThisId = value;
			}
		}
		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
			}
		}
		protected override void Render(HtmlTextWriter writer)
		{
			string sURL = HttpContext.Current.Request.RawUrl;
			string tmp = DataCache.GetTemplate("AddThis.txt");
			if (! (string.IsNullOrEmpty(AddThisId)))
			{
				tmp = tmp.Replace("[USERNAME]", AddThisId.Replace("'", "\\'"));
				tmp = tmp.Replace("[URL]", sURL);
				tmp = tmp.Replace("[TITLE]", Title.Replace("'", "\\'"));
				writer.Write(tmp);
			}

		}

	}

}
