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

namespace YAF.Controls
{
    #region Using

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.UI.WebControls;

    using YAF.Core.BaseControls;
    using YAF.Core.Extensions;
    using YAF.Core.Helpers;
    using YAF.Core.Model;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Services;
    using YAF.Types.Models;
    using YAF.Types.Objects.Model;
    using YAF.Web.Controls;

    #endregion

    /// <summary>
    /// My Topics List Control.
    /// </summary>
    public partial class MyTopicsList : BaseUserControl
    {
        #region Constants and Fields

        /// <summary>
        ///   default since date is now
        /// </summary>
        private DateTime sinceDate = DateTime.UtcNow;

        /// <summary>
        ///   default since option is "since last visit"
        /// </summary>
        private int sinceValue;

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets a value indicating whether Auto Data bind.
        /// </summary>
        public bool AutoDataBind { get; set; }

        /// <summary>
        ///   Gets or sets Determines what is the current mode of the control.
        /// </summary>
        public TopicListMode CurrentMode { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// The bind data.
        /// </summary>
        public void BindData()
        {
            // default since date is now
            this.sinceDate = DateTime.UtcNow;

            // default since option is "since last visit"
            this.sinceValue = 0;

            // is any "since"option selected
            if (this.Since.SelectedItem != null)
            {
                // get selected value
                this.sinceValue = int.Parse(this.Since.SelectedItem.Value);

                // decrypt selected option
                if (this.sinceValue == 9999)
                {
                    // all
                    // get all, from the beginning
                    this.sinceDate = DateTimeHelper.SqlDbMinTime();
                }
                else if (this.sinceValue > 0)
                {
                    // days
                    // get posts newer then defined number of days
                    this.sinceDate = DateTime.UtcNow - TimeSpan.FromDays(this.sinceValue);
                }
                else if (this.sinceValue < 0)
                {
                    // hours
                    // get posts newer then defined number of hours
                    this.sinceDate = DateTime.UtcNow + TimeSpan.FromHours(this.sinceValue);
                }
            }

            // we want to filter topics since last visit
            if (this.sinceValue == 0)
            {
                this.sinceDate = this.Get<ISession>().LastVisit ?? DateTime.UtcNow;

                if (this.CurrentMode.Equals(TopicListMode.Unread))
                {
                    this.sinceDate = this.Get<IReadTrackCurrentUser>().LastRead;
                }
            }

            // we'll hold topics in this table
            List<PagedTopic> topicList = null;

            var basePageSize = this.PageSize.SelectedValue.ToType<int>();

            this.PagerTop.PageSize = basePageSize;

            // page index in db which is returned back  is +1 based!
            var currentPageIndex = this.PagerTop.CurrentPageIndex;

            this.IconHeader.LocalizedPage = "MyTopics";

            // now depending on mode fill the table
            switch (this.CurrentMode)
            {
                case TopicListMode.Active:
                    this.IconHeader.LocalizedTag = "ActiveTopics";

                    topicList = this.GetRepository<Topic>().ListActivePaged(
                        this.PageContext.PageUserID,
                        this.sinceDate,
                        DateTime.UtcNow,
                        currentPageIndex,
                        basePageSize,
                        this.PageContext.BoardSettings.UseReadTrackingByDatabase);
                    break;
                case TopicListMode.Unanswered:
                    this.IconHeader.LocalizedTag = "UnansweredTopics";

                    topicList = this.GetRepository<Topic>().ListUnansweredPaged(
                        this.PageContext.PageUserID,
                        this.sinceDate,
                        DateTime.UtcNow,
                        currentPageIndex,
                        basePageSize,
                        this.PageContext.BoardSettings.UseReadTrackingByDatabase);
                    break;
                case TopicListMode.Unread:
                    this.IconHeader.LocalizedTag = "UnreadTopics";

                    topicList = this.GetRepository<Topic>().ListUnreadPaged(
                        this.PageContext.PageUserID,
                        this.sinceDate,
                        DateTime.UtcNow,
                        currentPageIndex,
                        basePageSize,
                        this.PageContext.BoardSettings.UseReadTrackingByDatabase);
                    break;
                case TopicListMode.User:
                    this.IconHeader.LocalizedTag = "MyTopics";

                    topicList = this.GetRepository<Topic>().ListByUserPaged(
                        this.PageContext.PageUserID,
                        this.sinceDate,
                        DateTime.UtcNow,
                        currentPageIndex,
                        basePageSize,
                        this.PageContext.BoardSettings.UseReadTrackingByDatabase);
                    break;
                case TopicListMode.Favorite:
                    this.IconHeader.LocalizedTag = "FavoriteTopics";

                    topicList = this.GetRepository<FavoriteTopic>().ListPaged(
                        this.PageContext.PageUserID,
                        this.sinceDate,
                        DateTime.UtcNow,
                        currentPageIndex,
                        basePageSize,
                        this.PageContext.BoardSettings.UseReadTrackingByDatabase);
                    break;
            }

            if (topicList == null)
            {
                this.PagerTop.Count = 0;
                return;
            }

            if (!topicList.Any())
            {
                this.PagerTop.Count = 0;
                this.TopicList.DataSource = null;
                this.TopicList.DataBind();
                return;
            }

            // let's page the results
            this.PagerTop.Count = topicList.FirstOrDefault().TotalRows;

            this.TopicList.DataSource = topicList;
            this.TopicList.DataBind();

            // Get new Feeds links
            this.BindFeeds();

            // data bind controls
            this.DataBind();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes dropdown with options to filter results by date.
        /// </summary>
        protected void InitSinceDropdown()
        {
            var lastVisit = this.Get<ISession>().LastVisit;

            // value 0, for since last visit
            this.Since.Items.Add(
                new ListItem(
                    this.GetTextFormatted(
                        "last_visit",
                        this.Get<IDateTimeService>().FormatDateTime(
                            lastVisit.HasValue && lastVisit.Value != DateTimeHelper.SqlDbMinTime()
                                ? lastVisit.Value
                                : DateTime.UtcNow)),
                    "0"));

            // negative values for hours backward
            this.Since.Items.Add(new ListItem(this.GetText("last_hour"), "-1"));
            this.Since.Items.Add(new ListItem(this.GetText("last_two_hours"), "-2"));
            this.Since.Items.Add(new ListItem(this.GetText("last_eight_hours"), "-8"));

            // positive values for days backward
            this.Since.Items.Add(new ListItem(this.GetText("last_day"), "1"));
            this.Since.Items.Add(new ListItem(this.GetText("last_two_days"), "2"));
            this.Since.Items.Add(new ListItem(this.GetText("last_week"), "7"));
            this.Since.Items.Add(new ListItem(this.GetText("last_two_weeks"), "14"));
            this.Since.Items.Add(new ListItem(this.GetText("last_month"), "31"));
        }

        /// <summary>
        /// Mark all Topics in the List as Read
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void MarkAll_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.BindData();

            if (this.TopicList.Items.Count <= 0)
            {
                return;
            }

            this.TopicList.Items.Cast<RepeaterItem>()
                .Where(item => item.ItemType is ListItemType.Item or ListItemType.AlternatingItem)
                .ForEach(
                    item => this.Get<IReadTrackCurrentUser>().SetTopicRead(item.DataItem.ToType<PagedTopic>().TopicID));

            // Rebind
            this.BindData();
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (!this.IsPostBack)
            {
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

                this.InitSinceDropdown();

                int? previousSince = null;

                this.FilterHolder.Visible = true;

                switch (this.CurrentMode)
                {
                    case TopicListMode.User:
                        previousSince = this.Get<ISession>().UserTopicSince;
                        this.Since.Items.Add(new ListItem(this.GetText("show_all"), "9999"));
                        this.Since.SelectedIndex = this.Since.Items.Count - 1;
                        break;
                    case TopicListMode.Unread:
                        previousSince = this.Get<ISession>().UnreadTopicSince;
                        this.Since.Items.Clear();
                        this.Since.Items.Add(new ListItem(this.GetText("SHOW_UNREAD_ONLY"), "0"));
                        this.Since.SelectedIndex = 0;

                        this.FilterHolder.Visible = false;
                        break;
                    case TopicListMode.Active:
                        previousSince = this.Get<ISession>().ActiveTopicSince;
                        this.Since.SelectedIndex = 0;
                        break;
                    case TopicListMode.Unanswered:
                        previousSince = this.Get<ISession>().UnansweredTopicSince;
                        this.Since.Items.Add(new ListItem(this.GetText("show_all"), "9999"));
                        this.Since.SelectedIndex = 2;
                        break;
                    case TopicListMode.Favorite:
                        previousSince = this.Get<ISession>().FavoriteTopicSince;
                        this.Since.Items.Add(new ListItem(this.GetText("show_all"), "9999"));
                        this.Since.SelectedIndex = this.Since.Items.Count - 1;
                        break;
                }

                // has user selected any "since" value before we can remember now?
                if (previousSince.HasValue)
                {
                    // look for value previously selected
                    var sinceItem = this.Since.Items.FindByValue(previousSince.Value.ToString());

                    // and select it if found
                    if (sinceItem != null)
                    {
                        this.Since.SelectedIndex = this.Since.Items.IndexOf(sinceItem);
                    }
                }
            }

            if (this.AutoDataBind)
            {
                this.BindData();
            }
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
        /// Handles the PageChange event of the Pager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Pager_PageChange([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.BindData();
        }

        /// <summary>
        /// Reloads the Topic Last Based on the Selected Since Value
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Since_SelectedIndexChanged([NotNull] object sender, [NotNull] EventArgs e)
        {
            // Set the controls' pager index to 0.
            this.PagerTop.CurrentPageIndex = 0;

            // save since option to remember it next time
            switch (this.CurrentMode)
            {
                case TopicListMode.User:
                    this.Get<ISession>().UserTopicSince = this.Since.SelectedValue.ToType<int>();
                    break;
                case TopicListMode.Unread:
                    this.Get<ISession>().UnreadTopicSince = this.Since.SelectedValue.ToType<int>();
                    break;
                case TopicListMode.Active:
                    this.Get<ISession>().ActiveTopicSince = this.Since.SelectedValue.ToType<int>();
                    break;
                case TopicListMode.Unanswered:
                    this.Get<ISession>().UnansweredTopicSince = this.Since.SelectedValue.ToType<int>();
                    break;
                case TopicListMode.Favorite:
                    this.Get<ISession>().FavoriteTopicSince = this.Since.SelectedValue.ToType<int>();
                    break;
            }

            // re-bind data
            this.BindData();
        }

        /// <summary>
        /// The create topic line.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        protected string CreateTopicLine(object item)
        {
            var topicLine = new TopicContainer { Item = item as PagedTopic };

            return topicLine.RenderToString();
        }

        /// <summary>
        /// Bind the Feeds
        /// </summary>
        private void BindFeeds()
        {
            var accessFavorite = this.Get<IPermissions>().Check(this.PageContext.BoardSettings.FavoriteTopicFeedAccess);

            // RSS link setup
            if (!this.PageContext.BoardSettings.ShowAtomLink)
            {
                return;
            }

            if (this.CurrentMode == TopicListMode.Favorite)
            {
                this.RssFeed.FeedType = RssFeeds.Favorite;
                this.RssFeed.Visible = accessFavorite;
            }
            else
            {
                this.RssFeed.Visible = false;
            }
        }

        #endregion
    }
}