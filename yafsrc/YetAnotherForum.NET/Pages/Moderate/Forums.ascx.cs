﻿/* Yet Another Forum.NET
 * Copyright (C) 2003-2005 Bjørnar Henden
 * Copyright (C) 2006-2013 Jaben Cargman
 * Copyright (C) 2014-2021 Ingo Herbote
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

namespace YAF.Pages.Moderate
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.UI.WebControls;

    using YAF.Core.BasePages;
    using YAF.Core.Helpers;
    using YAF.Core.Model;
    using YAF.Core.Services;
    using YAF.Core.Utilities;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Web.Controls;
    using YAF.Web.Extensions;

    using Forum = YAF.Types.Models.Forum;

    #endregion

    /// <summary>
    /// Forum Moderating Page.
    /// </summary>
    public partial class Forums : ModerateForumPage
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Forums" /> class.
        /// </summary>
        public Forums()
            : base("MODERATING")
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// The On PreRender event.
        /// </summary>
        /// <param name="e">
        /// the Event Arguments
        /// </param>
        protected override void OnPreRender([NotNull] EventArgs e)
        {
            this.PageContext.PageElements.RegisterJsBlockStartup(
                "TopicStarterPopoverJs",
                JavaScriptBlocks.TopicLinkPopoverJs(
                    $"{this.GetText("TOPIC_STARTER")}&nbsp;...",
                    ".topic-starter-popover",
                    "hover"));

            this.PageContext.PageElements.RegisterJsBlockStartup(
                "TopicLinkPopoverJs",
                JavaScriptBlocks.TopicLinkPopoverJs(
                    $"{this.GetText("LASTPOST")}&nbsp;{this.GetText("SEARCH", "BY")} ...",
                    ".topic-link-popover",
                    "focus hover"));

            base.OnPreRender(e);
        }

        /// <summary>
        /// Handles the Click event of the AddUser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void AddUser_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.ModForumUserDialog.BindData(this.PageContext.PageForumID, null);

            this.PageContext.PageElements.RegisterJsBlockStartup(
                "openModalJs",
                JavaScriptBlocks.OpenModalJs("ModForumUserDialog"));
        }

        /// <summary>
        /// Binds the data
        /// </summary>
        protected void BindData()
        {
            this.PagerTop.PageSize = this.PageSize.SelectedValue.ToType<int>();

            var baseSize = this.PagerTop.PageSize;
            var currentPageIndex = this.PagerTop.CurrentPageIndex;

            var topicList = this.GetRepository<Topic>().ListPaged(
                this.PageContext.PageForumID,
                this.PageContext.PageUserID,
                DateTimeHelper.SqlDbMinTime(),
                DateTime.UtcNow,
                currentPageIndex,
                baseSize,
                true,
                this.PageContext.BoardSettings.UseReadTrackingByDatabase);

            this.topiclist.DataSource = topicList;
            this.UserList.DataSource = this.GetRepository<UserForum>().List(null, this.PageContext.PageForumID);

            if (!topicList.NullOrEmpty())
            {
                this.PagerTop.Count = topicList.FirstOrDefault().TotalRows;
            }

            var forumList = this.GetRepository<Forum>().ListAllSorted(
                this.PageContext.PageBoardID,
                this.PageContext.PageUserID);

            this.ForumList.AddForumAndCategoryIcons(forumList);

            this.DataBind();

            var pageItem = this.ForumList.Items.FindByValue(this.PageContext.PageForumID.ToString());

            if (pageItem != null)
            {
                pageItem.Selected = true;
            }
        }

        /// <summary>
        /// Deletes all the Selected Topics
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void DeleteTopics_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            var list = this.GetSelectedTopics();

            if (!list.Any())
            {
                this.PageContext.AddLoadMessage(this.GetText("MODERATE", "NOTHING"), MessageTypes.warning);
            }
            else
            {
                list.ForEach(x => this.GetRepository<Topic>().Delete(this.PageContext.PageForumID, x.TopicRowID.Value));

                this.PageContext.AddLoadMessage(this.GetText("moderate", "deleted"), MessageTypes.success);

                this.BindData();
            }
        }

        /// <summary>
        /// Handles the Click event of the Move control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Move_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            int? linkDays = null;
            var ld = -2;

            if (this.LeavePointer.Checked && this.LinkDays.Text.IsSet() && !int.TryParse(this.LinkDays.Text, out ld))
            {
                this.PageContext.AddLoadMessage(this.GetText("POINTER_DAYS_INVALID"), MessageTypes.warning);
                return;
            }

            if (this.ForumList.SelectedValue.ToType<int>() <= 0)
            {
                this.PageContext.AddLoadMessage(this.GetText("CANNOT_MOVE_TO_CATEGORY"), MessageTypes.warning);
                return;
            }

            // only move if it's a destination is a different forum.
            if (this.ForumList.SelectedValue.ToType<int>() != this.PageContext.PageForumID)
            {
                if (ld >= -2)
                {
                    linkDays = ld;
                }

                var list = this.GetSelectedTopics();

                if (!list.Any())
                {
                    this.PageContext.AddLoadMessage(this.GetText("MODERATE", "NOTHING"), MessageTypes.warning);
                }
                else
                {
                    list.ForEach(
                        x => this.GetRepository<Topic>().Move(
                            x.TopicRowID.Value,
                            this.PageContext.PageForumID,
                            this.ForumList.SelectedValue.ToType<int>(),
                            this.LeavePointer.Checked,
                            linkDays.Value));

                    this.PageContext.AddLoadMessage(this.GetText("MODERATE", "MOVED"), MessageTypes.success);

                    this.BindData();
                }
            }
            else
            {
                this.PageContext.AddLoadMessage(this.GetText("MODERATE", "MOVE_TO_DIFFERENT"), MessageTypes.danger);
            }
        }

        /// <summary>
        /// The page_ load.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (!this.PageContext.ForumModeratorAccess)
            {
                this.Get<LinkBuilder>().AccessDenied();
            }

            if (!this.PageContext.IsForumModerator && !this.PageContext.IsAdmin)
            {
                this.ModerateUsersHolder.Visible = false;
            }

            if (!this.IsPostBack)
            {
                var showMoved = this.PageContext.BoardSettings.ShowMoved;

                // Ederon : 7/14/2007 - by default, leave pointer is set on value defined on host level
                this.LeavePointer.Checked = showMoved;

                this.trLeaveLink.Visible = showMoved;
                this.trLeaveLinkDays.Visible = showMoved;

                if (showMoved)
                {
                    this.LinkDays.Text = "1";
                }
            }

            this.PageSize.DataSource = StaticDataHelper.PageEntries();
            this.PageSize.DataTextField = "Name";
            this.PageSize.DataValueField = "Value";
            this.PageSize.DataBind();

            try
            {
                this.PageSize.SelectedValue = this.PageContext.User.PageSize.ToString();
            }
            catch (Exception)
            {
                this.PageSize.SelectedValue = "5";
            }

            this.BindData();
        }

        /// <summary>
        /// The page size on selected index changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void PageSizeSelectedIndexChanged(object sender, EventArgs e)
        {
            this.BindData();
        }

        /// <summary>
        /// Create the Page links.
        /// </summary>
        protected override void CreatePageLinks()
        {
            if (this.PageContext.Settings.LockedForum == 0)
            {
                this.PageLinks.AddRoot();
                this.PageLinks.AddCategory(this.PageContext.PageCategory.Name, this.PageContext.PageCategoryID);
            }

            this.PageLinks.AddForum(this.PageContext.PageForumID);
            this.PageLinks.AddLink(this.GetText("MODERATE", "TITLE"), string.Empty);
        }

        /// <summary>
        /// The pager top_ page change.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void PagerTop_PageChange([NotNull] object sender, [NotNull] EventArgs e)
        {
            // rebind
            this.BindData();
        }

        /// <summary>
        /// The user list_ item command.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void UserList_ItemCommand([NotNull] object source, [NotNull] RepeaterCommandEventArgs e)
        {
            switch (e.CommandName)
            {
                case "edit":
                    this.ModForumUserDialog.BindData(this.PageContext.PageForumID, e.CommandArgument.ToType<int>());

                    this.PageContext.PageElements.RegisterJsBlockStartup(
                        "openModalJs",
                        JavaScriptBlocks.OpenModalJs("ModForumUserDialog"));
                    break;
                case "remove":
                    this.GetRepository<UserForum>().Delete(e.CommandArgument.ToType<int>(), this.PageContext.PageForumID);

                    this.BindData();

                    break;
            }
        }

        /// <summary>
        /// Gets the selected topics
        /// </summary>
        /// <returns>
        /// Returns the List of selected Topics
        /// </returns>
        private List<TopicContainer> GetSelectedTopics()
        {
            var list = new List<TopicContainer>();

            this.topiclist.Items.Cast<RepeaterItem>().ForEach(item =>
                {
                    var check = item.FindControlAs<CheckBox>("topicCheck");

                    var topicContainer = item.FindControlAs<TopicContainer>("topicContainer");

                    if (check.Checked)
                    {
                        list.Add(topicContainer);
                    }
                });

            return list;
        }

        #endregion
    }
}