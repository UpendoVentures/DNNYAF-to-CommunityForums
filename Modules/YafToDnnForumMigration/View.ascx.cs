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
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Exceptions;
using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Upendo.Modules.YafToDnnForumMigration.Components;
using Upendo.Modules.YafToDnnForumMigration.Entities;
using YAF.Types.Interfaces;
using YAF.Core;
using YAF.Types.Models;
using YAF.Core.Extensions;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Collections;
using DotNetNuke.Common.Utilities;
using System.Web.UI.WebControls;
using YAF.Core.Model;
using System.Data;
using DotNetNuke.Modules.ActiveForums;

namespace Upendo.Modules.YafToDnnForumMigration
{
    public partial class View : PortalModuleBase, IHaveServiceLocator
    {
        public IServiceLocator ServiceLocator { get; set; }

        public View()
        {
            this.ServiceLocator = BoardContext.Current.ServiceLocator;
        }

        #region Event Handlers

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                FillYAFForumList();
                FillActiveForumsList();
            }
        }

        private void FillYAFForumList()
        {
            var dt = this.GetRepository<Board>().GetAll();

            this.BoardID.DataSource = dt;
            this.BoardID.DataTextField = "Name";
            this.BoardID.DataValueField = "ID";

            this.BoardID.DataBind();
        }

        private void FillActiveForumsList()
        {
            var objTabController = new TabController();

            var objDesktopModuleInfo =
                DesktopModuleController.GetDesktopModuleByModuleName("Active Forums", this.PortalId);

            if (objDesktopModuleInfo is null)
            {
                this.ActiveForumsPlaceHolder.Visible = false;
                return;
            }

            var tabs = TabController.GetPortalTabs(this.PortalSettings.PortalId, -1, true, true);

            tabs.Where(tab => !tab.IsDeleted).ForEach(
                tabInfo =>
                {
                    var moduleController = new ModuleController();

                    var tabModules = moduleController.GetTabModules(tabInfo.TabID).Select(pair => pair.Value).Where(
                    m => !m.IsDeleted && m.DesktopModuleID == objDesktopModuleInfo.DesktopModuleID);

                    tabModules.ForEach(
                    moduleInfo =>
                            {
                                var path = tabInfo.TabName;
                                var tabSelected = tabInfo;

                                while (tabSelected.ParentId != Null.NullInteger)
                                {
                                    tabSelected = objTabController.GetTab(tabSelected.ParentId, tabInfo.PortalID, false);
                                    if (tabSelected is null)
                                    {
                                        break;
                                    }

                                    path = $"{tabSelected.TabName} -> {path}";
                                }

                                var objListItem = new ListItem
                                {
                                    Value = moduleInfo.ModuleID.ToString(),
                                    Text = $@"{path} -> {moduleInfo.ModuleTitle}"
                                };

                                this.ActiveForums.Items.Add(objListItem);
                            });
                });

            if (this.ActiveForums.Items.Count == 0)
            {
                this.ActiveForumsPlaceHolder.Visible = false;
            }
        }

        protected void lnkImport_Click(object sender, EventArgs e)
        {
            try
            {
                var asyncTask = Task.Factory.StartNew(
                DoImport,
                new ImportConfiguration
                {
                    HttpContext = Context,
                    Session = Session,
                    DnnPortalSettings = PortalSettings.Current
                });

                ScriptManager.RegisterStartupScript(this, GetType(), "importProgress", "importProgress();", true);
            }
            catch (Exception ex)
            {
                DotNetNuke.Services.Exceptions.Exceptions.ProcessModuleLoadException(this, ex, IsEditable);
            }
        }

        protected void DoImport(object objConfiguration)
        {
            var conf = objConfiguration as ImportConfiguration;

            var manager = new SessionManager(conf.Session);
            manager.AdminProductImportLog = null;
            manager.AdminProductImportProgress = 0;

            Progress(conf.DnnPortalSettings, (p, l) =>
            {
                lock (manager.AdminProductImportLog)
                {
                    manager.AdminProductImportProgress = Math.Round(p * 100);

                    if (!string.IsNullOrEmpty(l))
                    {
                        var log = manager.AdminProductImportLog;
                        log.Add(l);
                        manager.AdminProductImportLog = log;
                    }
                }
            });
        }

        public List<ForumGroupInfo> ActiveForumGroups { get; set; }

        private void Progress(PortalSettings portalSettings, Action<double, string> log)
        {
            try
            {
                log(0, $@"
*************************************************
**  Start Migration (this may take some time)  **
*************************************************");

                int activeForumModuleId = Convert.ToInt32(ActiveForums.SelectedValue);

                var objModules = new DotNetNuke.Entities.Modules.ModuleController();
                var objSettings = new SettingsInfo { MainSettings = objModules.GetModule(activeForumModuleId).ModuleSettings };
                var activeForumTheme = objSettings.Theme;
                string themePath = Page.ResolveUrl("~/DesktopModules/ActiveForums/themes/" + activeForumTheme);

                var importForum = new ImportForum(portalSettings, activeForumModuleId, themePath, log);
                var yafModels = new YafModel()
                {
                    users = this.GetRepository<YAF.Types.Models.User>().GetAll(),
                    categories = this.GetRepository<Category>().GetAll(),
                    forums = this.GetRepository<YAF.Types.Models.Forum>().GetAll(),
                    topics = this.GetRepository<Topic>().GetAll(),
                    messages = this.GetRepository<Message>().GetAll()
                };

                log(0, $@"
--- Total Data to import ---
    Users:   {yafModels.users.Count}
    Group:   {yafModels.categories.Count}
    Forum:   {yafModels.forums.Count}
    Topic:   {yafModels.topics.Count}
    Message: {yafModels.messages.Count}");

                log(0, $@"----------------------------------------------------------");
                log(0, $@"| Group  | Forum (topics / replies)");
                log(0, $@"----------------------------------------------------------");
                foreach (var category in yafModels.categories)
                {
                    var forums = yafModels.forums.Where(x => x.CategoryID == category.ID).OrderBy(x => x.SortOrder);
                    foreach (var forum in forums)
                    {
                        var forumTopics = yafModels.topics.Where(x => x.ForumID == forum.ID);
                        int messageCount = 0;
                        foreach (var topic in forumTopics)
                        {
                            var topicMessages = yafModels.messages.Where(x => x.TopicID == topic.ID);
                            messageCount += topicMessages.Count();
                        }
                        log(0, $@"| {category.Name} | {forum.Name} ({forumTopics.Count().ToString()} / {messageCount.ToString()})");
                    }
                }
                log(0, $@"----------------------------------------------------------");

                importForum.Progress(yafModels);

                log(1, "--- Import finished ---");
            }
            catch (Exception ex)
            {
                log(1, "Import failed:" + ex.Message);
            }
        }

        #endregion

        private class ImportConfiguration
        {
            public HttpContext HttpContext { get; set; }
            public HttpSessionState Session { get; set; }
            public PortalSettings DnnPortalSettings { get; set; }
        }
    }
}