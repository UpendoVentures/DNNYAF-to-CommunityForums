using DotNetNuke.Modules.ActiveForums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using YAF.Core;
using YAF.Types.Interfaces;
using YAF.Types.Models;
using DotNetNuke;
using System.Collections;
using DotNetNuke.Entities.Portals;
using System.Text.RegularExpressions;
using YAF.Types.Extensions;

namespace Upendo.Modules.YafToDnnForumMigration.Entities
{
    public class ImportForum: IHaveServiceLocator
    {
        public ImportForum(PortalSettings portalSettings, int activeForumModuleId, string activeForumThemePath, Action<double, string> Log)
        {
            _portalSettings = portalSettings;
            _portalId = portalSettings.PortalId;
            _activeForumModuleId = activeForumModuleId;
            _themePath = activeForumThemePath;
            log = Log;
        }

        private PortalSettings _portalSettings { get; set; }
        private int _portalId { get; set; }
        private int _activeForumModuleId { get; set; }
        private string _themePath { get; set; }
        private Action<double, string> log { get; set; }
        private double logNum { get; set; }
        private List<ForumGroupInfo> ActiveForumGroups { get; set; }

        List<YafDNNUser> dnnYafUsers { get; set; }

        public IServiceLocator ServiceLocator => throw new NotImplementedException();

        public CountVM Counter { get; set; }

        public void Progress(YafModel data)
        {
            Counter = new CountVM()
            {
                User = new SuccessFailureVM(),
                Group = new SuccessFailureVM(),
                Forum = new SuccessFailureVM(),
                Topic = new SuccessFailureVM(),
                Message = new SuccessFailureVM(),
            };
            
            try
            {
                int count = data.categories.Count + data.forums.Count + data.topics.Count;

                ActiveForumGroups = Groups_List(_activeForumModuleId);

                log(0.1, "--- Start migrating users");
                dnnYafUsers = GetDNNUserID();
                int j = 0;
                foreach (var usr in data.users)
                {
                    try {
                        if (!string.IsNullOrEmpty(usr.ProviderUserKey))
                        {
                            j++;
                            var u = dnnYafUsers.FirstOrDefault(x => x.ProviderUserKey == usr.ProviderUserKey);
                            ImportUsers(u.DnnUserID, usr);
                            Counter.User.Success++;
                        }
                        else
                            Counter.User.Fail++;
                    }
                    catch (Exception ex) {
                        Counter.User.Fail++;
                        log(0.1, "Problem importing user: " + usr.DisplayName);
                        throw;
                    }
                }
                log(0.15, "    " + data.users.Count() + " users imported");

                int i = 0;
                foreach (var category in data.categories)
                {
                    i++;
                    logNum = 0.15 + ((i * 0.85) / count);//0.10 + 0.01 * i / data.categories.Count();
                    log(logNum, "--- Create Group: " + category.Name);
                    var groupId = Group_Save(_activeForumModuleId, _portalId, category);
                    if (groupId > 0)
                    {
                        var forums = data.forums.Where(x => x.CategoryID == category.ID).OrderBy(x => x.SortOrder);
                        foreach (var forum in forums)
                        {
                            i++;
                            logNum = 0.15 + ((i * 0.85) / count);//0.10 + 0.001 * i / forums.Count();
                            log(logNum, "+ forum: " + forum.Name);
                            var forumId = Forum_Save(_activeForumModuleId, _portalId, groupId, forum);
                            if (forumId > 0)
                            {
                                var forumTopics = data.topics.Where(x => x.ForumID == forum.ID);
                                foreach (var topic in forumTopics)
                                {
                                    i++;
                                    logNum = 0.15 + ((i * 0.85) / count);//0.10 + 0.0001 * i / forumTopics.Count();
                                    log(logNum, "- " + topic.TopicName);
                                    var topicMessages = data.messages.Where(x => x.TopicID == topic.ID);
                                    if (topicMessages.Count() > 0)
                                    {
                                        // Keep continue import, event if there's an error.
                                        try
                                        {
                                            // UserId = 1 is giving error due it ProviderKey is NULL
                                            if (topic.UserID != 1)
                                            {
                                                int topicId = Topic_Save(forumId, topic, topicMessages.ToList());
                                                SaveReplies(forumId, topicId, topicMessages.ToList());
                                            }
                                            else
                                                Counter.Topic.Fail++;
                                        }
                                        catch
                                        {
                                            log(logNum, $"Problem importing Topic: {topic.TopicName} ({topic.ID})");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                log(1, $@"
--- Data Imported Successfully ---
    Users:   {Counter.User.Success}
    Group:   {Counter.Group.Success}
    Forum:   {Counter.Forum.Success}
    Topic:   {Counter.Topic.Success}
    Message: {Counter.Message.Success}");
                log(1, $@"
--- Data Imported Failed ---
    Users:   {Counter.User.Fail}
    Group:   {Counter.Group.Fail}
    Forum:   {Counter.Forum.Fail}
    Topic:   {Counter.Topic.Fail}
    Message: {Counter.Message.Fail}");
            }
            catch (Exception ex)
            {
                log(1, "--- Import failed:" + ex.Message);
            }
        }

        #region Users

        private void ImportUsers(int userId, YAF.Types.Models.User user)
        {
            // Create a new User if not exist.
            DataProvider.Instance().Profiles_UpdateActivity(_portalId, _activeForumModuleId, userId);

            UserProfileController upc = new UserProfileController();
            //UserController uc = new UserController();
            UserProfileInfo up = upc.Profiles_Get(_portalId, _activeForumModuleId, userId);

            //UserProfileInfo up = uc.GetUser(portalId, forumModuleId, userId).Profile;
            up.Signature = user.Signature;
            up.TopicCount = 0;
            up.ReplyCount = 0;
            up.ViewCount = 0;
            up.AnswerCount = 0;
            up.RewardPoints = 0;
            //up.UserCaption = "";
            up.Signature = user.Signature;
            up.SignatureDisabled = false;
            up.Avatar = user.Avatar;
            up.RewardPoints = user.Points;
            up.DateCreated = user.Joined;
            up.DateUpdated = user.LastVisit;
            up.DateLastActivity = user.LastVisit;
            upc.Profiles_Save(up);
        }

        private List<YafDNNUser> GetDNNUserID()
        {
            string sp = "upendo_ForumMigration_GetDNNUserID";
            var result = Upendo.Modules.YafToDnnForumMigration.Entities.DAL<YafDNNUser>.ExecuteQuery(System.Data.CommandType.StoredProcedure, sp).ToList();
            return result;
        }

        private DotNetNuke.Modules.ActiveForums.User GetDNNUser(int userId)
        {
            DotNetNuke.Modules.ActiveForums.User u = new DotNetNuke.Modules.ActiveForums.User();
            DotNetNuke.Entities.Users.UserController uc = new DotNetNuke.Entities.Users.UserController();
            DotNetNuke.Entities.Users.UserInfo dnnUser = uc.GetUser(_portalSettings.PortalId, userId);
            u = LoadUser(dnnUser);
            return u;
        }

        internal DotNetNuke.Modules.ActiveForums.User LoadUser(DotNetNuke.Entities.Users.UserInfo dnnUser)
        {
            DotNetNuke.Modules.ActiveForums.User u = new DotNetNuke.Modules.ActiveForums.User();
            DotNetNuke.Entities.Users.UserInfo cu = dnnUser;

            u.UserId = cu.UserID;
            u.UserName = cu.Username;
            u.IsSuperUser = cu.IsSuperUser;
            u.IsAdmin = cu.IsInRole(_portalSettings.AdministratorRoleName);
            u.DateCreated = cu.Membership.CreatedDate;
            u.DateUpdated = cu.Membership.LastActivityDate;
            u.FirstName = cu.FirstName;
            u.LastName = cu.LastName;
            u.DisplayName = cu.DisplayName;
            u.Email = cu.Email;
            u.UserRoles = GetRoleIds(cu, _portalSettings.PortalId); //RolesToString(cu.Roles)

            if (cu.IsSuperUser)
            {
                u.UserRoles += Globals.DefaultAnonRoles + _portalSettings.AdministratorRoleId + ";";
            }
            Social social = new Social();
            u.UserRoles += "|" + cu.UserID + "|";

            if (!cu.IsSuperUser)
            {
                u.Properties = GetUserProperties(dnnUser);
            }

            return u;
        }

        private string GetRoleIds(DotNetNuke.Entities.Users.UserInfo u, int SiteId)
        {
            string RoleIds = string.Empty;
            DotNetNuke.Security.Roles.RoleController rc = new DotNetNuke.Security.Roles.RoleController();
            foreach (DotNetNuke.Security.Roles.RoleInfo r in rc.GetRoles(SiteId))
            {
                string roleName = r.RoleName;
                foreach (string role in u.Roles)
                {
                    if (!string.IsNullOrEmpty(role))
                    {
                        if (roleName == role)
                        {
                            RoleIds += r.RoleID.ToString() + ";";
                            break;
                        }
                    }
                }
            }

            foreach (DotNetNuke.Security.Roles.RoleInfo r in u.Social.Roles)
            {
                RoleIds += r.RoleID.ToString() + ";";
            }

            return RoleIds;
        }

        public Hashtable GetUserProperties(DotNetNuke.Entities.Users.UserInfo dnnUser)
        {
            Hashtable ht = new Hashtable();
            foreach (DotNetNuke.Entities.Profile.ProfilePropertyDefinition up in dnnUser.Profile.ProfileProperties)
            {
                ht.Add(up.PropertyName, up.PropertyValue);
            }
            return ht;
        }

        #endregion

        #region Groups

        private List<ForumGroupInfo> Groups_List(int moduleId)
        {
            var fgc = new ForumGroupController();
            var groupArr = fgc.Groups_List(moduleId);
            List<ForumGroupInfo> groups = new List<ForumGroupInfo>();
            if (groupArr.Count > 0)
            {
                foreach (ForumGroupInfo group in groupArr)
                {
                    groups.Add(group);
                }
            }
            return groups;
        }

        private void Group_RemoveAll(int moduleId)
        {
            var fgc = new ForumGroupController();
            var groupArr = fgc.Groups_List(moduleId);
            foreach (ForumGroupInfo group in groupArr)
            {
                fgc.Group_Delete(moduleId, group.ForumGroupId);
            }
        }

        private int Group_Save(int activeForumModuleId, int portalId, Category cat)
        {
            try
            {
                var bIsNew = false;
                var groupExist = ActiveForumGroups.FirstOrDefault(x => x.GroupName == cat.Name);
                var groupId = groupExist != null ? groupExist.ForumGroupId : 0;

                var fgc = new ForumGroupController();
                var gi = (groupId > 0) ? fgc.Groups_Get(activeForumModuleId, groupId) : new ForumGroupInfo();

                var securityKey = string.Empty;
                if (groupId == 0)
                    bIsNew = true;
                else
                    securityKey = "G:" + groupId;

                gi.ModuleId = activeForumModuleId;
                gi.ForumGroupId = groupId;
                gi.GroupName = cat.Name;
                gi.Active = true;
                gi.Hidden = false;

                gi.SortOrder = cat.SortOrder;

                gi.PrefixURL = string.Empty;

                gi.GroupSettingsKey = securityKey;
                var gc = new ForumGroupController();
                groupId = gc.Groups_Save(portalId, gi, bIsNew);

                Counter.Group.Success++;
                return groupId;
            }
            catch (Exception ex)
            {
                Counter.Group.Fail++;
                return -1;
            }
        }

        #endregion

        #region Forums

        private int Forum_Save(int activeForumModuleId, int portalId, int groupId, YAF.Types.Models.Forum forum)
        {
            try
            {
                DotNetNuke.Entities.Modules.ModuleController objModules = new DotNetNuke.Entities.Modules.ModuleController();
                var moduleInfo = objModules.GetModule(activeForumModuleId);
                Hashtable htSettings = moduleInfo.TabModuleSettings; // objModules.GetTabModuleSettings(activeForumModuleId);

                var fi = new DotNetNuke.Modules.ActiveForums.Forum();
                var fc = new ForumController();
                var forumDr = DotNetNuke.Modules.ActiveForums.DataProvider.Instance().Forums_List(portalId, activeForumModuleId, groupId, 0, false);
                int forumId = -1;

                List<DotNetNuke.Modules.ActiveForums.Forum> forums = new List<DotNetNuke.Modules.ActiveForums.Forum>();
                while (forumDr.Read())
                {
                    forums.Add(new DotNetNuke.Modules.ActiveForums.Forum()
                    {
                        ForumID = Convert.ToInt32(forumDr["ForumID"]),
                        ForumName = Convert.ToString(forumDr["ForumName"]),
                        ForumGroupId = Convert.ToInt32(forumDr["ForumGroupID"])
                    });
                }
                forumDr.Close();

                var bIsNew = false;
                var existingForum = forums.FirstOrDefault(x => x.ForumName == forum.Name.Replace(" - ", ""));
                forumId = existingForum != null ? existingForum.ForumID : -1;
                bIsNew = forumId > 0 ? false : true;

                if (bIsNew)
                    forumId = fc.CreateGroupForum(portalId, activeForumModuleId, 0, groupId, forum.Name.Replace(" - ", ""), forum.Description, forum.IsHidden.HasValue ? forum.IsHidden.Value : false, "");
                //else
                //    forumId = fc.Forums_Save(portalId, existingForum, false, true);

                // Remove All Topics to prevent duplicate
                var tc = new TopicsController();
                var forumTopics = DAL<string>.ExecuteQuery(System.Data.CommandType.Text, "Select TopicId from activeforums_ForumTopics Where ForumId = @0", forumId);
                foreach(var t in forumTopics)
                {
                    Topic_Delete(Convert.ToInt32(t));
                }

                DataCache.ClearForumGroupsCache(activeForumModuleId);

                var cachekey = string.Format("AF-FI-{0}-{1}-{2}", portalId, activeForumModuleId, forumId);
                DataCache.CacheClear(cachekey);

                cachekey = string.Format("AF-FV-{0}-{1}", portalId, activeForumModuleId);
                DataCache.CacheClearPrefix(cachekey);

                Counter.Forum.Success++;
                return forumId;
            }
            catch (Exception ex)
            {
                Counter.Forum.Fail++;
                return -1;
            }
        }

        #endregion

        #region Topics

        private SettingsInfo MainSettings(int forumModuleId)
        {
            var objModules = new DotNetNuke.Entities.Modules.ModuleController();
            var objSettings = new SettingsInfo { MainSettings = objModules.GetModule(forumModuleId).ModuleSettings };

            return objSettings;
        }

        private void Topic_Delete(int topicId)
        {
            int TopicId = topicId;

            int forumId = -1;
            DotNetNuke.Modules.ActiveForums.Data.ForumsDB db = new DotNetNuke.Modules.ActiveForums.Data.ForumsDB();
            forumId = db.Forum_GetByTopicId(TopicId);
            
            DataProvider.Instance().Topics_Delete(forumId, TopicId, 0);
        }

        private int Topic_Save(int forumId, YAF.Types.Models.Topic topic, List<YAF.Types.Models.Message> topicMessages)
        {
            try
            {
                var fc = new ForumController();
                var forum = fc.GetForum(_portalId, _activeForumModuleId, forumId);
                var usr = dnnYafUsers.FirstOrDefault(x => x.YafUserID == topic.UserID);
                int authorId = usr.DnnUserID;

                var message = topicMessages.FirstOrDefault(x => x.Position == 0);

                var subject = string.IsNullOrEmpty(topic.TopicName) ? "" : topic.TopicName;
                var body = string.IsNullOrEmpty(message.MessageText) ? "" : message.MessageText;
                subject = Utilities.CleanString(_portalId, subject, false, EditorTypes.TEXTBOX, forum.UseFilter, false, _activeForumModuleId, _themePath, false);

                // Yaf Formating...
                body = RepairHtml(body, true);

                body = Utilities.CleanString(_portalId, body, forum.AllowHTML, forum.EditorType, forum.UseFilter, forum.AllowScript, _activeForumModuleId, _themePath, forum.AllowEmoticons);
                var summary = string.Empty;

                if (topic.ID == 99)
                {
                    var c = topic.ID;
                }

                var tc = new TopicsController();
                TopicInfo ti = new TopicInfo();
                var topicId = -1;
                var bIsNew = false;

                string sp = "upendo_ForumMigration_GetForumTopic";
                var forumTopics = DAL<ForumTopicsVM>.ExecuteQuery(System.Data.CommandType.StoredProcedure, sp, forumId).ToList();
                if (forumTopics.Count > 0)
                {
                    var existingForum = forumTopics.FirstOrDefault(x => x.Subject == subject);
                    if (existingForum != null)
                        topicId = existingForum.TopicId;
                        //ti = tc.Topics_Get(portalId, activeForumModuleId, existingForum.TopicId);
                }

                bIsNew = topicId > 0 ? false : true;
                if (bIsNew)
                {
                    var dt = DateTime.Now;
                    ti.Content.DateCreated = topic.Posted;
                    ti.Content.DateUpdated = topic.LastPosted.HasValue ? topic.LastPosted.Value : topic.Posted;

                    ti.AnnounceEnd = Utilities.NullDate();
                    ti.AnnounceStart = Utilities.NullDate();
                    ti.Priority = topic.Priority;

                    if (bIsNew)
                    {
                        ti.Content.AuthorId = authorId;
                        ti.Content.AuthorName = usr.DisplayName;
                        ti.Content.IPAddress = message.IP;
                        ti.Content.IsDeleted = message.IsDeleted.HasValue ? message.IsDeleted.Value : false;
                    }

                    ti.TopicUrl = string.Empty;

                    ti.Content.Subject = subject;
                    ti.Content.Body = body;
                    ti.Content.Summary = string.Empty;
                    ti.IsAnnounce = ti.AnnounceEnd != Utilities.NullDate() && ti.AnnounceStart != Utilities.NullDate();
                    ti.ViewCount = topic.Views;

                    ti.IsApproved = message.IsApproved.HasValue ? message.IsApproved.Value : true;
                    ti.IsArchived = false;
                    ti.IsDeleted = topic.IsDeleted.HasValue ? topic.IsDeleted.Value : false;
                    ti.IsLocked = false;
                    ti.IsPinned = false;
                    ti.StatusId = -1;
                    ti.TopicIcon = string.Empty;
                    ti.TopicType = 0;
                    ti.TopicData = string.Empty;

                    topicId = tc.TopicSave(_portalId, ti);
                }

                ti = tc.Topics_Get(_portalId, _activeForumModuleId, topicId, forumId, -1, false);
                if (ti != null)
                {
                    tc.Topics_SaveToForum(forumId, topicId, _portalId, _activeForumModuleId);
                    //SaveAttachments(ti.ContentId);
                    if (ti.IsApproved && ti.Author.AuthorId > 0)
                    {
                        var uc = new DotNetNuke.Modules.ActiveForums.Data.Profiles();
                        uc.Profile_UpdateTopicCount(_portalId, ti.Author.AuthorId);
                    }
                }

                if (!(Subscriptions.IsSubscribed(_portalId, _activeForumModuleId, forumId, topicId, SubscriptionTypes.Instant, authorId)))
                {
                    var user = GetDNNUser(usr.DnnUserID);

                    var sc = new SubscriptionController();
                    sc.Subscription_Update(_portalId, _activeForumModuleId, forumId, topicId, 1, authorId, user.UserRoles);
                }

                Counter.Topic.Success++;
                Counter.Message.Success++;

                return topicId;
            }
            catch (Exception ex)
            {
                Counter.Topic.Fail++;
                log(logNum, $"Problem importing Topic: {topic.TopicName} ({topic.ID})");
                log(logNum, "Errors: " + ex.Message);
                return -1;
                //throw;
            }
        }

        #endregion

        #region Reply Message

        private void SaveReplies(int activeForumId, int activeForumTopicId, List<YAF.Types.Models.Message> yafTopicMessages)
        {
            var tc = new TopicsController();
            var topic = tc.Topics_Get(_portalId, _activeForumModuleId, activeForumTopicId);

            // Topic NULL because it been mark as Deleted on YafForum
            if (topic != null)
            {
                foreach(var replyMsg in yafTopicMessages.OrderBy(x => x.Position)) {
                    if (replyMsg.Position != 0)
                    {
                        int replyToId = 0;
                        var replyTo = yafTopicMessages.FirstOrDefault(x => x.ID == replyMsg.ReplyTo);
                        if (replyTo.Position != 0 || replyTo.ReplyTo.HasValue)
                            replyToId = replyTo.ID;

                        var id = Reply_Save(activeForumId, activeForumTopicId, topic, replyMsg, replyToId);
                        if (id > 0)
                            Counter.Message.Success++;
                        else
                        {
                            log(logNum, $"Problem importing message: {replyMsg.MessageText} ({replyMsg.ID})");
                            Counter.Message.Fail++;
                            throw new Exception();
                        }
                    }
                }
            }
        }

        private int Reply_Save(int activeForumId, int activeForumTopicId, TopicInfo topic, YAF.Types.Models.Message msg, int replyToId)
        {
            var fc = new ForumController();
            var forum = fc.GetForum(_portalId, _activeForumModuleId, activeForumId);

            var subject = "RE: " + topic.Content.Subject;
            var body = string.IsNullOrEmpty(msg.MessageText) ? "" : msg.MessageText;
            subject = Utilities.CleanString(_portalId, subject, false, EditorTypes.TEXTBOX, forum.UseFilter, false, _activeForumModuleId, _themePath, false);
            // Yaf Formating...
            body = RepairHtml(body, true);
            body = Utilities.CleanString(_portalId, body, forum.AllowHTML, forum.EditorType, forum.UseFilter, forum.AllowScript, _activeForumModuleId, _themePath, forum.AllowEmoticons);

            var usr = dnnYafUsers.FirstOrDefault(x => x.YafUserID == msg.UserID);
            int authorId = usr.DnnUserID;
            string authorName = Utilities.CleanString(_portalId, usr.DisplayName, false, EditorTypes.TEXTBOX, true, false, _activeForumModuleId, _themePath, false);
            
            if (authorName.Trim() == string.Empty || subject.Trim() == string.Empty)
                return -1;

            var tc = new TopicsController();
            var rc = new ReplyController();
            ReplyInfo ri = new ReplyInfo();
            var dt = DateTime.Now;
            ri.Content.DateCreated = msg.Posted;
            ri.Content.DateUpdated = msg.Posted;

            ri.Content.AuthorId = authorId;
            ri.Content.AuthorName = authorName;
            ri.Content.IPAddress = msg.IP;

            if (Regex.IsMatch(body, "<CODE([^>]*)>", RegexOptions.IgnoreCase))
            {
                foreach (Match m in Regex.Matches(body, "<CODE([^>]*)>(.*?)</CODE>", RegexOptions.IgnoreCase))
                    body = body.Replace(m.Value, m.Value.Replace("<br>", System.Environment.NewLine));
            }

            ri.Content.Body = body;
            ri.Content.Subject = subject;
            ri.Content.Summary = string.Empty;

            ri.IsApproved = msg.IsApproved.HasValue ? msg.IsApproved.Value : false;

            var bSend = ri.IsApproved;
            ri.IsDeleted = false;
            ri.StatusId = -1; //ctlForm.StatusId;
            ri.TopicId = activeForumTopicId;
            ri.ReplyToId = replyToId;

            var tmpReplyId = rc.Reply_Save(_portalId, ri);
            ri = rc.Reply_Get(_portalId, _activeForumModuleId, activeForumTopicId, tmpReplyId);
            //SaveAttachments(ri.ContentId);
            
            var cachekey = string.Format("AF-FV-{0}-{1}", _portalId, _activeForumModuleId);
            DataCache.CacheClearPrefix(cachekey);
            try
            {
                if (!(Subscriptions.IsSubscribed(_portalId, _activeForumModuleId, activeForumId, activeForumTopicId, SubscriptionTypes.Instant, authorId)))
                {
                    var user = GetDNNUser(usr.DnnUserID);

                    var sc = new SubscriptionController();
                    sc.Subscription_Update(_portalId, _activeForumModuleId, activeForumId, activeForumTopicId, 1, authorId, user.UserRoles);
                }
                return tmpReplyId;
            }
            catch (Exception ex)
            {
                log(logNum, $"Error importing message: {msg.MessageText} ({msg.ID})");
                log(logNum, $"Errors: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region private

        public string RepairHtml([YAF.Types.NotNull] string html, bool allowHtml)
        {
            // vzrus: NNTP temporary tweaks to wipe out server hangs. Put it here as it can be in every place.
            // These are '\n\r' things related to multiline regexps.
            var mc1 = Regex.Matches(html, "[^\r]\n[^\r]", RegexOptions.IgnoreCase);
            for (var i = mc1.Count - 1; i >= 0; i--)
            {
                html = html.Insert(mc1[i].Index + 1, " \r");
            }

            var mc2 = Regex.Matches(html, "[^\r]\n\r\n[^\r]", RegexOptions.IgnoreCase);
            for (var i = mc2.Count - 1; i >= 0; i--)
            {
                html = html.Insert(mc2[i].Index + 1, " \r");
            }

            string AcceptedHTML = "br,hr,b,i,u,a,div,ol,ul,li,blockquote,img,span,p,em,strong,font,pre,h1,h2,h3,h4,h5,h6,address";
            html = !allowHtml
                       ? html//this.HttpServer.HtmlEncode(html)
                       : RemoveHtmlByList(html, AcceptedHTML.Split(','));

            return html;
        }

        Func<int, int> square = x => x * x;

        private static string RemoveHtmlByList([YAF.Types.NotNull] string text, [YAF.Types.NotNull] IEnumerable<string> matchList)
        {
            var allowedTags = matchList.ToList();
            
            MatchAndPerformAction(
                "<.*?>",
                text,
                (tag, index, len) =>
                {
                    if (!IsValidTag(tag, allowedTags))
                    {
                        text = text.Remove(index, len);
                    }
                });

            return text;
        }

        private static void MatchAndPerformAction(
        [YAF.Types.NotNull] string matchRegEx,
        [YAF.Types.NotNull] string text,
        [YAF.Types.NotNull] Action<string, int, int> matchAction)
        {
            const RegexOptions RegexOptions = RegexOptions.IgnoreCase;

            var matches = Regex.Matches(text, matchRegEx, RegexOptions).Cast<Match>().OrderByDescending(x => x.Index);

            matches.ForEach(
                match =>
                {
                    var inner = text.Substring(match.Index + 1, match.Length - 1).Trim().ToLower();
                    matchAction(inner, match.Index, match.Length);
                });
        }

        public static bool IsValidTag([YAF.Types.CanBeNull] string tag, IEnumerable<string> allowedTags)
        {
            if (tag.IndexOf("javascript", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            if (tag.IndexOf("vbscript", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            if (tag.IndexOf("onclick", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            var endchars = new[] { ' ', '>', '/', '\t' };

            var pos = tag.IndexOfAny(endchars, 1);
            if (pos > 0)
            {
                tag = tag.Substring(0, pos);
            }

            if (tag[0] == '/')
            {
                tag = tag.Substring(1);
            }

            // check if it's a valid tag
            return allowedTags.Any(allowedTag => tag == allowedTag);
        }

        #endregion

    }

    #region Class Models

    public class YafModel
    {
        public List<YAF.Types.Models.Category> categories { get; set; }
        public List<YAF.Types.Models.Forum> forums { get; set; }
        public List<YAF.Types.Models.Topic> topics { get; set; }
        public List<YAF.Types.Models.Message> messages { get; set; }
        public List<YAF.Types.Models.User> users { get; set; }
    }

    public class YafDNNUser
    {
        public string ProviderUserKey { get; set; }
        public int YafUserID { get; set; }
        public int DnnUserID { get; set; }
        public string DisplayName { get; set; }
        public string UserName { get; set; }
    }

    public class SuccessFailureVM
    {
        public int Success { get; set; }
        public int Fail { get; set; }
    }
    public class CountVM
    {
        public SuccessFailureVM User { get; set; }
        public SuccessFailureVM Group { get; set; }
        public SuccessFailureVM Forum { get; set; }
        public SuccessFailureVM Topic { get; set; }
        public SuccessFailureVM Message { get; set; }
    }

    public class ForumTopicsVM
    {
        public int ForumId { get; set; }
        public int TopicId { get; set; }
        public int ContentId { get; set; }
        public string Subject { get; set; }
    }

    #endregion
}