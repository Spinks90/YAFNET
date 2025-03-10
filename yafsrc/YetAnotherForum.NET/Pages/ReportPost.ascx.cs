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

namespace YAF.Pages
{
    #region Using

    using System;
    using System.Web;

    using YAF.Core.BasePages;
    using YAF.Core.Extensions;
    using YAF.Core.Model;
    using YAF.Core.Services;
    using YAF.Core.Utilities;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Web.Extensions;

    #endregion

    /// <summary>
    /// The form for reported post complaint text.
    /// </summary>
    public partial class ReportPost : ForumPage
    {
        #region Constants and Fields

        /// <summary>
        ///   The _all posts by user.
        /// </summary>
        private Tuple<Topic, Message, User, Forum> message;

        /// <summary>
        /// The topic name.
        /// </summary>
        private string topicName;

        #endregion

        /// <summary>
        ///   Gets AllPostsByUser.
        /// </summary>
        public Tuple<Topic, Message, User, Forum> Message =>
            this.message ??= this.GetRepository<Message>().GetMessageWithAccess(this.MessageId, this.PageContext.PageUserID);

        protected int MessageId => this.Get<LinkBuilder>().StringToIntOrRedirect(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("m"));

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the ReportPost class.
        /// </summary>
        public ReportPost()
            : base("REPORTPOST")
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Return to Post
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void CancelClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            // Redirect to reported post
            this.RedirectToPost();
        }

        /// <summary>
        /// Report Message.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void ReportClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (!this.Page.IsValid)
            {
                return;
            }

            if (this.Report.Text.Length > this.PageContext.BoardSettings.MaxReportPostChars)
            {
                this.PageContext.AddLoadMessage(
                    this.GetTextFormatted("REPORTTEXT_TOOLONG", this.PageContext.BoardSettings.MaxReportPostChars),
                    MessageTypes.danger);

                return;
            }

            // Save the reported message
            this.GetRepository<MessageReported>().Report(
                this.Message,
                this.PageContext.PageUserID,
                DateTime.UtcNow,
                this.Report.Text);

            // Send Notification to Mods about the Reported Post.
            if (this.PageContext.BoardSettings.EmailModeratorsOnReportedPost)
            {
                // not approved, notify moderators
                this.Get<ISendNotification>().ToModeratorsThatMessageWasReported(
                    this.PageContext.PageForumID,
                    this.MessageId,
                    this.PageContext.PageUserID,
                    this.Report.Text);
            }

            this.PageContext.LoadMessage.AddSession(this.GetText("MSG_REPORTED"), MessageTypes.success);

            // Redirect to reported post
            this.RedirectToPost();
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (this.Get<HttpRequestBase>().QueryString.Exists("m"))
            {
                // We check here if the user have access to the option
                if (!this.Get<IPermissions>().Check(this.PageContext.BoardSettings.ReportPostPermissions))
                {
                    this.Get<LinkBuilder>().Redirect(ForumPages.Info, "i=1");
                }
            }

            this.PageContext.PageElements.RegisterJsBlockStartup(
                nameof(JavaScriptBlocks.FormValidatorJs),
                JavaScriptBlocks.FormValidatorJs(this.btnReport.ClientID));

            if (this.IsPostBack)
            {
                return;
            }

            // Checking if the user has a right to view the message and getting data
            if (this.Message != null)
            {
                this.topicName = this.Message.Item1.TopicName;

                this.MessagePreview.CurrentMessage = this.Message.Item2;

                this.UserLink1.Suspended = this.Message.Item3.Suspended;
                this.UserLink1.ReplaceName = this.Message.Item3.DisplayOrUserName();
                this.UserLink1.Style = this.Message.Item3.UserStyle;
                this.UserLink1.UserID = this.Message.Item3.ID;

                this.Posted.DateTime = this.Message.Item2.Posted;
            }
            else
            {
                this.Get<LinkBuilder>().Redirect(ForumPages.Info, "i=1");
            }

            this.Report.MaxLength = this.PageContext.BoardSettings.MaxReportPostChars;

            this.LocalizedLblMaxNumberOfPost.Param0 = this.PageContext.BoardSettings.MaxReportPostChars.ToString();
        }

        /// <summary>
        /// The create page links.
        /// </summary>
        protected override void CreatePageLinks()
        {
            // Get Forum Link
            this.PageLinks.AddRoot();
        }

        /// <summary>
        /// Redirects to reported post after Save or Cancel
        /// </summary>
        protected void RedirectToPost()
        {
            // Redirect to reported post
            this.Get<LinkBuilder>().Redirect(ForumPages.Posts, "m={0}&name={1}", this.MessageId, this.topicName);
        }

        #endregion
    }
}