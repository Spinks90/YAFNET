/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2020 Ingo Herbote
 * https://www.yetanotherforum.net/
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at

 * https://www.apache.org/licenses/LICENSE-2.0

 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

namespace YAF.Pages.Admin
{
    #region Using

    using System;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.UI.WebControls;

    using YAF.Configuration;
    using YAF.Core;
    using YAF.Core.BasePages;
    using YAF.Core.Extensions;
    using YAF.Core.Helpers;
    using YAF.Core.Model;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Utils;
    using YAF.Utils.Helpers;
    using YAF.Web.Extensions;

    using ListItem = System.Web.UI.WebControls.ListItem;

    #endregion

    /// <summary>
    /// The Board Settings Admin Page.
    /// </summary>
    public partial class Settings : AdminPage
    {
        #region Methods

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (this.IsPostBack)
            {
                return;
            }

            var boardSettings = this.Get<BoardSettings>();

            this.CdvVersion.Text = boardSettings.CdvVersion.ToString();

            // create list boxes by populating data sources from Data class
            var themeData = StaticDataHelper.Themes();

            if (themeData.Any())
            {
                this.Theme.DataSource = themeData;
            }

            this.Culture.DataSource = StaticDataHelper.Cultures().OrderBy(x => x.CultureNativeName);

            this.Culture.DataTextField = "CultureNativeName";
            this.Culture.DataValueField = "CultureTag";

            this.ShowTopic.DataSource = StaticDataHelper.TopicTimes();
            this.ShowTopic.DataTextField = "Name";
            this.ShowTopic.DataValueField = "Value";

            this.BindData();

            // population default notification setting options...
            var items = EnumHelper.EnumToDictionary<UserNotificationSetting>();

            if (!boardSettings.AllowNotificationAllPostsAllTopics)
            {
                // remove it...
                items.Remove(UserNotificationSetting.AllTopics.ToInt());
            }

            var notificationItems = items.Select(
                    x => new ListItem(
                        HtmlHelper.StripHtml(this.GetText("SUBSCRIPTIONS", x.Value)),
                        x.Key.ToString()))
                .ToArray();

            this.DefaultNotificationSetting.Items.AddRange(notificationItems);

            // Get first default full culture from a language file tag.
            var langFileCulture = StaticDataHelper.CultureDefaultFromFile(boardSettings.Language) ?? "en-US";

            if (boardSettings.Theme.Contains(".xml"))
            {
                SetSelectedOnList(ref this.Theme, "yaf");
            }
            else
            {
                SetSelectedOnList(ref this.Theme, boardSettings.Theme);
            }

            SetSelectedOnList(ref this.Culture, boardSettings.Culture);
            if (this.Culture.SelectedIndex == 0)
            {
                // If 2-letter language code is the same we return Culture, else we return  a default full culture from language file
                SetSelectedOnList(
                    ref this.Culture,
                    langFileCulture.Substring(0, 2) == boardSettings.Culture ? boardSettings.Culture : langFileCulture);
            }

            SetSelectedOnList(ref this.ShowTopic, boardSettings.ShowTopicsDefault.ToString());
            SetSelectedOnList(
                ref this.DefaultNotificationSetting,
                boardSettings.DefaultNotificationSetting.ToInt().ToString());

            this.NotificationOnUserRegisterEmailList.Text = boardSettings.NotificationOnUserRegisterEmailList;
            this.EmailModeratorsOnModeratedPost.Checked = boardSettings.EmailModeratorsOnModeratedPost;
            this.EmailModeratorsOnReportedPost.Checked = boardSettings.EmailModeratorsOnReportedPost;
            this.AllowDigestEmail.Checked = boardSettings.AllowDigestEmail;
            this.DefaultSendDigestEmail.Checked = boardSettings.DefaultSendDigestEmail;
            this.ForumEmail.Text = boardSettings.ForumEmail;
            this.ForumBaseUrlMask.Text = boardSettings.BaseUrlMask;

            var item = this.BoardLogo.Items.FindByText(boardSettings.ForumLogo);

            if (item != null)
            {
                item.Selected = true;
            }

            this.CopyrightRemovalKey.Text = boardSettings.CopyrightRemovalDomainKey;

            this.DigestSendEveryXHours.Text = boardSettings.DigestSendEveryXHours.ToString();

            // Copyright Link-back Algorithm
            // Please keep if you haven't purchased a removal or commercial license.
            this.CopyrightHolder.Visible = true;

            // Render board Announcement

            // add items to the dropdown
            this.BoardAnnouncementUntilUnit.Items.Add(new ListItem(this.GetText("PROFILE", "MONTH"), "3"));
            this.BoardAnnouncementUntilUnit.Items.Add(new ListItem(this.GetText("PROFILE", "DAYS"), "1"));
            this.BoardAnnouncementUntilUnit.Items.Add(new ListItem(this.GetText("PROFILE", "HOURS"), "2"));

            // select hours
            this.BoardAnnouncementUntilUnit.SelectedIndex = 0;

            // default number of hours to suspend user for
            this.BoardAnnouncementUntil.Text = "1";

            if (boardSettings.BoardAnnouncement.IsNotSet())
            {
                return;
            }

            this.CurrentAnnouncement.Visible = true;
            this.CurrentMessage.Text =
                $"{this.GetText("ANNOUNCEMENT_CURRENT")}:&nbsp;{boardSettings.BoardAnnouncementUntil}";
            this.BoardAnnouncementType.SelectedValue = boardSettings.BoardAnnouncementType;
            this.BoardAnnouncement.Text = boardSettings.BoardAnnouncement;
        }

        /// <summary>
        /// Creates page links for this page.
        /// </summary>
        protected override void CreatePageLinks()
        {
            this.PageLinks.AddLink(this.Get<BoardSettings>().Name, BuildLink.GetLink(ForumPages.Board));
            this.PageLinks.AddLink(
                this.GetText("ADMIN_ADMIN", "Administration"),
                BuildLink.GetLink(ForumPages.Admin_Admin));
            this.PageLinks.AddLink(this.GetText("ADMIN_BOARDSETTINGS", "TITLE"), string.Empty);

            this.Page.Header.Title =
                $"{this.GetText("ADMIN_ADMIN", "Administration")} - {this.GetText("ADMIN_BOARDSETTINGS", "TITLE")}";
        }

        /// <summary>
        /// Save the Board Settings
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void SaveClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            var languageFile = "english.xml";

            var cultures = StaticDataHelper.Cultures()
                .Where(c => c.CultureTag.Equals(this.Culture.SelectedValue)).ToList();

            if (cultures.Any())
            {
                languageFile = cultures.FirstOrDefault().CultureFile;
            }

            this.GetRepository<Board>().Save(
                this.PageContext.PageBoardID,
                this.Name.Text,
                languageFile,
                this.Culture.SelectedValue);

            // save poll group
            var boardSettings = this.Get<BoardSettings>();

            boardSettings.Language = languageFile;
            boardSettings.Culture = this.Culture.SelectedValue;
            boardSettings.Theme = this.Theme.SelectedValue;

            // allow null/empty as a mobile theme many not be desired.
            boardSettings.ShowTopicsDefault = this.ShowTopic.SelectedValue.ToType<int>();
            boardSettings.NotificationOnUserRegisterEmailList = this.NotificationOnUserRegisterEmailList.Text.Trim();

            boardSettings.EmailModeratorsOnModeratedPost = this.EmailModeratorsOnModeratedPost.Checked;
            boardSettings.EmailModeratorsOnReportedPost = this.EmailModeratorsOnReportedPost.Checked;
            boardSettings.AllowDigestEmail = this.AllowDigestEmail.Checked;
            boardSettings.DefaultSendDigestEmail = this.DefaultSendDigestEmail.Checked;
            boardSettings.DefaultNotificationSetting =
                this.DefaultNotificationSetting.SelectedValue.ToEnum<UserNotificationSetting>();

            boardSettings.ForumEmail = this.ForumEmail.Text;

            if (this.BoardLogo.SelectedIndex > 0)
            {
                boardSettings.ForumLogo = this.BoardLogo.SelectedItem.Text;
            }

            boardSettings.BaseUrlMask = this.ForumBaseUrlMask.Text;
            boardSettings.CopyrightRemovalDomainKey = this.CopyrightRemovalKey.Text.Trim();

            if (!int.TryParse(this.DigestSendEveryXHours.Text, out var hours))
            {
                hours = 24;
            }

            boardSettings.DigestSendEveryXHours = hours;

            // save the settings to the database
            ((LoadBoardSettings)boardSettings).SaveRegistry();

            // Reload forum settings
            this.PageContext.BoardSettings = null;

            // Clearing cache with old users permissions data to get new default styles...
            this.Get<IDataCache>().Remove(x => x.StartsWith(Constants.Cache.ActiveUserLazyData));

            BuildLink.Redirect(ForumPages.Admin_Admin);
        }

        /// <summary>
        /// Saves the Board Announcement
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void SaveAnnouncementClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            var boardAnnouncementUntil = DateTime.Now;

            // number inserted by suspending user
            var count = int.Parse(this.BoardAnnouncementUntil.Text);

            // what time units are used for suspending
            boardAnnouncementUntil = this.BoardAnnouncementUntilUnit.SelectedValue switch
                {
                    // days
                    "1" => boardAnnouncementUntil.AddDays(count),
                    // hours
                    "2" => boardAnnouncementUntil.AddHours(count),
                    // month
                    "3" => boardAnnouncementUntil.AddMonths(count),
                    _ => boardAnnouncementUntil
                };

            var boardSettings = this.Get<BoardSettings>();

            boardSettings.BoardAnnouncementUntil = boardAnnouncementUntil.ToString(CultureInfo.InvariantCulture);
            boardSettings.BoardAnnouncement = this.BoardAnnouncement.Text;
            boardSettings.BoardAnnouncementType = this.BoardAnnouncementType.SelectedValue;

            // save the settings to the database
            ((LoadBoardSettings)boardSettings).SaveRegistry();

            // Reload forum settings
            this.PageContext.BoardSettings = null;

            BuildLink.Redirect(ForumPages.Admin_Settings);
        }

        /// <summary>
        /// Deletes the Announcement
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void DeleteClick(object sender, EventArgs e)
        {
            var boardSettings = this.Get<BoardSettings>();

            boardSettings.BoardAnnouncementUntil = DateTime.MinValue.ToString(CultureInfo.InvariantCulture);
            boardSettings.BoardAnnouncement = this.BoardAnnouncement.Text;
            boardSettings.BoardAnnouncementType = this.BoardAnnouncementType.SelectedValue;

            // save the settings to the database
            ((LoadBoardSettings)boardSettings).SaveRegistry();

            // Reload forum settings
            this.PageContext.BoardSettings = null;

            BuildLink.Redirect(ForumPages.Admin_Settings);
        }

        /// <summary>
        /// Increases the CDV version on click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void IncreaseVersionOnClick(object sender, EventArgs e)
        {
            this.Get<BoardSettings>().CdvVersion++;
            ((LoadBoardSettings)this.Get<BoardSettings>()).SaveRegistry();

            this.CdvVersion.Text = this.Get<BoardSettings>().CdvVersion.ToString();
        }

        /// <summary>
        /// The set selected on list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="value">The value.</param>
        private static void SetSelectedOnList([NotNull] ref DropDownList list, [NotNull] string value)
        {
            var selItem = list.Items.FindByValue(value);

            list.ClearSelection();

            if (selItem != null)
            {
                selItem.Selected = true;
            }
            else if (list.Items.Count > 0)
            {
                // select the first...
                list.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
        private void BindData()
        {
            var board = this.GetRepository<Board>().GetById(this.PageContext.PageBoardID);

            using (var dt = new DataTable("Files"))
            {
                dt.Columns.Add("FileName", typeof(string));
                dt.Columns.Add("Description", typeof(string));
                
                var dr = dt.NewRow();
                dr["FileName"] =
                    BoardInfo.GetURLToContent("images/spacer.gif"); // use spacer.gif for Description Entry
                dr["Description"] = this.GetText("BOARD_LOGO_SELECT");
                dt.Rows.Add(dr);

                var dir = new DirectoryInfo(
                    this.Get<HttpRequestBase>()
                        .MapPath($"{BoardInfo.ForumServerFileRoot}{BoardFolders.Current.Logos}"));
                var files = dir.GetFiles("*.*");

                dt.AddImageFiles(files, BoardFolders.Current.Logos);

                this.BoardLogo.DataSource = dt;
                this.BoardLogo.DataValueField = "FileName";
                this.BoardLogo.DataTextField = "Description";
            }

            this.DataBind();
            this.Name.Text = board.Name;
        }

        #endregion
    }
}