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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;

    using YAF.Core.BaseModules;
    using YAF.Core.BasePages;
    using YAF.Core.Extensions;
    using YAF.Core.Helpers;
    using YAF.Core.Model;
    using YAF.Core.Services;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Flags;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Identity;
    using YAF.Types.Models;
    using YAF.Types.Objects.Model;
    using YAF.Web.Extensions;

    using ListItem = System.Web.UI.WebControls.ListItem;

    #endregion

    /// <summary>
    /// The Post Private Message Page
    /// </summary>
    public partial class PostPrivateMessage : ForumPage
    {
        public PagedPm ReplyMessage
        {
            get => this.ViewState["ReplyMessage"].ToType<PagedPm>();

            set => this.ViewState["ReplyMessage"] = value;
        }

        #region Constants and Fields

        /// <summary>
        ///   message body editor
        /// </summary>
        private ForumEditor editor;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "PostPrivateMessage" /> class.
        ///   Default constructor.
        /// </summary>
        public PostPrivateMessage()
            : base("PMESSAGE")
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnInit([NotNull] EventArgs e)
        {
            if (this.PageContext.BoardSettings.AllowPrivateMessageAttachments)
            {
                this.PageContext.PageElements.AddScriptReference("FileUploadScript");

#if DEBUG
                this.PageContext.PageElements.RegisterCssIncludeContent("jquery.fileupload.comb.css");
#else
                this.PageContext.PageElements.RegisterCssIncludeContent("jquery.fileupload.comb.min.css");
#endif
            }

            base.OnInit(e);
        }

        /// <summary>
        /// Send pm to all users
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void AllUsers_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            // create one entry to show in dropdown
            var li = new ListItem(this.GetText("ALLUSERS"), "0");

            // bind the list to dropdown
            this.ToList.Items.Add(li);
            this.ToList.Visible = true;
            this.To.Text = this.GetText("ALLUSERS");

            // hide To text box
            this.To.Visible = false;

            // hide find users/all users buttons
            this.FindUsers.Visible = false;
            this.AllUsers.Visible = false;
            this.AllBuddies.Visible = false;

            // we need clear button now
            this.Clear.Visible = true;
        }

        /// <summary>
        /// Send PM to all Buddies
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void AllBuddies_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            // try to find users by user name
            var usersFound = this.Get<IFriends>().ListAll();

            var friendsString = new StringBuilder();

            if (!usersFound.Any())
            {
                return;
            }

            // we found a user(s)
            usersFound.ForEach(
                row => friendsString.AppendFormat(
                    "{0};",
                    this.PageContext.BoardSettings.EnableDisplayName ? row.DisplayName : row.Name));

            this.To.Text = friendsString.ToString();

            // hide find users/all users buttons
            this.FindUsers.Visible = false;
            this.AllUsers.Visible = false;
            this.AllBuddies.Visible = false;

            // we need clear button now
            this.Clear.Visible = true;
        }

        /// <summary>
        /// Redirect user back to his PM inbox
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Cancel_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.Get<LinkBuilder>().Redirect(ForumPages.MyMessages);
        }

        /// <summary>
        /// Clears the User List
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Clear_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            // clear drop down
            this.ToList.Items.Clear();

            // hide it and show empty To text box
            this.ToList.Visible = false;
            this.To.Text = string.Empty;
            this.To.Visible = true;

            // show find users and all users (if user is admin)
            this.FindUsers.Visible = true;
            this.AllUsers.Visible = this.PageContext.IsAdmin;
            this.AllBuddies.Visible = this.PageContext.UserHasBuddies && this.PageContext.BoardSettings.EnableBuddyList;

            // clear button is not necessary now
            this.Clear.Visible = false;
        }

        /// <summary>
        /// Creates page links for this page.
        /// </summary>
        protected override void CreatePageLinks()
        {
            // forum index
            this.PageLinks.AddRoot();

            // users control panel
            this.PageLinks.AddLink(this.PageContext.User.DisplayOrUserName(), this.Get<LinkBuilder>().GetLink(ForumPages.MyAccount));

            // private messages
            this.PageLinks.AddLink(
                this.GetText(ForumPages.MyMessages.ToString(), "TITLE"),
                this.Get<LinkBuilder>().GetLink(ForumPages.MyMessages));

            // post new message
            this.PageLinks.AddLink(this.GetText("TITLE"));
        }

        /// <summary>
        /// Find Users
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void FindUsers_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (this.To.Text.Length < 2)
            {
                // need at least 2 letters of user's name
                this.PageContext.AddLoadMessage(this.GetText("NEED_MORE_LETTERS"), MessageTypes.warning);
                return;
            }

            // try to find users by user name
            var usersFound = this.Get<IUserDisplayName>().FindUserContainsName(this.To.Text.Trim()).Where(
                u => !u.Block.BlockPMs && u.UserFlags.IsApproved && u.ID != this.PageContext.PageUserID).ToList();

            if (usersFound.Any())
            {
                // we found a user(s)
                this.ToList.DataSource = usersFound;
                this.ToList.DataValueField = "ID";
                this.ToList.DataTextField = this.PageContext.BoardSettings.EnableDisplayName ? "DisplayName" : "Name";
                this.ToList.DataBind();

                // ToList.SelectedIndex = 0;
                // hide To text box and show To drop down
                this.ToList.Visible = true;
                this.To.Visible = false;

                // find is no more needed
                this.FindUsers.Visible = false;

                // we need clear button displayed now
                this.Clear.Visible = true;
            }
            else
            {
                // user not found
                this.PageContext.AddLoadMessage(this.GetText("USER_NOTFOUND"), MessageTypes.danger);
                return;
            }

            // re-bind data to the controls
            this.DataBind();
        }

        /// <summary>
        /// Handles the Initialization event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Init([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.editor = ForumEditorHelper.GetCurrentForumEditor();

            this.editor.MaxCharacters = this.PageContext.BoardSettings.MaxPostSize;

            this.EditorLine.Controls.Add(this.editor);

            this.editor.UserCanUpload = this.PageContext.BoardSettings.AllowPrivateMessageAttachments;

            // add editor to the page
            this.EditorLine.Controls.Add(this.editor);
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
        {
            // if user isn't authenticated, redirect him to login page
            if (this.User == null || this.PageContext.IsGuest)
            {
                this.RedirectNoAccess();
            }

            // this needs to be done just once, not during post-backs
            if (this.IsPostBack)
            {
                return;
            }

            // only administrators can send messages to all users
            this.AllUsers.Visible = this.PageContext.IsAdmin;

            this.AllBuddies.Visible = this.PageContext.UserHasBuddies && this.PageContext.BoardSettings.EnableBuddyList;

            // Is Reply
            if (this.Get<HttpRequestBase>().QueryString.Exists("p"))
            {
                // PM is a reply or quoted reply (isQuoting)
                // to the given message id "p"
                var isQuoting = this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("q") == "1";

                var isReport = this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("report") == "1";

                // get quoted message
                this.ReplyMessage =
                    this.GetRepository<PMessage>().GetMessage(
                        this.Get<LinkBuilder>().StringToIntOrRedirect(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("p")));

                // there is such a message
                if (this.ReplyMessage == null)
                {
                    return;
                }

                // get message sender/recipient
                var toUserId = this.ReplyMessage.ToUserID;
                var fromUserId = this.ReplyMessage.FromUserID;

                // verify access to this PM
                if (toUserId != this.PageContext.PageUserID && fromUserId != this.PageContext.PageUserID)
                {
                    this.Get<LinkBuilder>().AccessDenied();
                }

                // handle subject
                var subject = this.ReplyMessage.Subject;
                if (!subject.StartsWith("Re: "))
                {
                    subject = $"Re: {subject}";
                }

                this.PmSubjectTextBox.Text = subject;

                var displayName = this.Get<IUserDisplayName>().GetNameById(fromUserId);

                // set "To" user and disable changing...
                this.To.Text = displayName;
                this.To.Enabled = false;
                this.FindUsers.Visible = false;
                this.AllUsers.Visible = false;
                this.AllBuddies.Visible = false;

                if (!isQuoting)
                {
                    return;
                }

                // PM is a quoted reply
                var body = this.ReplyMessage.Body;

                if (this.PageContext.BoardSettings.RemoveNestedQuotes)
                {
                    body = this.Get<IFormatMessage>().RemoveNestedQuotes(body);
                }

                // Ensure quoted replies have bad words removed from them
                body = this.Get<IBadWordReplace>().Replace(body);

                // Quote the original message
                body = $"[QUOTE={displayName}]{body}[/QUOTE]";

                // we don't want any whitespaces at the beginning of message
                this.editor.Text = body.TrimStart();

                if (!isReport)
                {
                    return;
                }

                var hostUser = this.GetRepository<User>().Get(u => u.BoardID == this.PageContext.PageBoardID && (u.Flags & 1) == 1).FirstOrDefault();

                if (hostUser != null)
                {
                    this.To.Text = hostUser.DisplayOrUserName();

                    this.PmSubjectTextBox.Text = this.GetTextFormatted("REPORT_SUBJECT", displayName);

                    var bodyReport = $"[QUOTE={displayName}]{this.ReplyMessage.Body}[/QUOTE]";

                    // Quote the original message
                    bodyReport = this.GetTextFormatted("REPORT_BODY", bodyReport);

                    // we don't want any whitespaces at the beginning of message
                    this.editor.Text = bodyReport.TrimStart();
                }
                else
                {
                    this.Get<LinkBuilder>().AccessDenied();
                }
            }
            else if (this.Get<HttpRequestBase>().QueryString.Exists("u")
                     && this.Get<HttpRequestBase>().QueryString.Exists("r"))
            {
                // PM is being send as a quoted reply to a reported post
                // We check here if the user have access to the option
                if (!this.PageContext.IsModeratorInAnyForum && !this.PageContext.IsForumModerator)
                {
                    return;
                }

                // get quoted message
                var reporter =
                        this.GetRepository<User>().MessageReporter(
                            this.Get<LinkBuilder>().StringToIntOrRedirect(
                                this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("r")),
                            this.Get<LinkBuilder>().StringToIntOrRedirect(
                                this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("u")))
                        .FirstOrDefault();

                // there is such a message
                // message info should be always returned as 1 row
                if (reporter == null)
                {
                    return;
                }

                // handle subject
                this.PmSubjectTextBox.Text = this.GetText("REPORTED_SUBJECT");

                var displayName =
                    this.Get<IUserDisplayName>().GetNameById(reporter.Item1.ID);

                // set "To" user and disable changing...
                this.To.Text = displayName;
                this.To.Enabled = false;
                this.FindUsers.Visible = false;
                this.AllUsers.Visible = false;
                this.AllBuddies.Visible = false;

                // Parse content with delimiter '|'
                var quoteList = reporter.Item2.ReportText.Split('|');

                // Quoted replies should have bad words in them
                // Reply to report PM is always a quoted reply
                // Quote the original message in a cycle
                for (var i = 0; i < quoteList.Length; i++)
                {
                    // Add quote codes
                    quoteList[i] = $"[QUOTE={displayName}]{quoteList[i]}[/QUOTE]\r\n";

                    // Replace DateTime delimiter '??' by ': '
                    // we don't want any whitespaces at the beginning of message
                    this.editor.Text = quoteList[i].Replace("??", ": ") + this.editor.Text.TrimStart();
                }
            }
            else if (this.Get<HttpRequestBase>().QueryString.Exists("u"))
            {
                // find user
                var foundUser =
                    this.GetRepository<User>().GetById(this.Get<LinkBuilder>().StringToIntOrRedirect(
                        this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("u")));

                if (foundUser == null)
                {
                    return;
                }

                if (foundUser.ID == this.PageContext.PageUserID)
                {
                    return;
                }

                this.To.Text = foundUser.DisplayOrUserName();

                this.To.Enabled = false;

                // hide find user/all users buttons
                this.FindUsers.Visible = false;
                this.AllUsers.Visible = false;
                this.AllBuddies.Visible = false;
            }
            else
            {
                // Blank PM

                // multi-receiver info is relevant only when sending blank PM
                if (this.PageContext.BoardSettings.PrivateMessageMaxRecipients < 1 || this.PageContext.IsAdmin)
                {
                    return;
                }

                // format localized string
                this.MultiReceiverInfo.Text =
                    $"{string.Format(this.GetText("MAX_RECIPIENT_INFO"), this.PageContext.BoardSettings.PrivateMessageMaxRecipients)} {this.GetText("MULTI_RECEIVER_INFO")}";

                // display info
                this.MultiReceiverAlert.Visible = true;
            }
        }

        /// <summary>
        /// Previews the Message Output
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Preview_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            // make preview row visible
            this.PreviewRow.Visible = true;

            this.PreviewMessagePost.MessageFlags.IsHtml = this.editor.UsesHTML;
            this.PreviewMessagePost.MessageFlags.IsBBCode = this.editor.UsesBBCode;
            this.PreviewMessagePost.Message = this.editor.Text;
            this.PreviewMessagePost.MessageID = 0;

            if (!this.PageContext.BoardSettings.AllowSignatures)
            {
                return;
            }

            var user = this.GetRepository<User>().GetById(this.PageContext.PageUserID);

            if (user.Signature.IsSet())
            {
                this.PreviewMessagePost.Signature = user.Signature;
            }
        }

        /// <summary>
        /// Send Private Message
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Save_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            var replyTo = this.Get<HttpRequestBase>().QueryString.Exists("p")
                ? this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAsInt("p")
                : null;

            // Check if quoted message is Reply
            if (this.ReplyMessage.ReplyTo.HasValue)
            {
                replyTo = this.ReplyMessage.ReplyTo;
            }

            // recipient was set in dropdown
            if (this.ToList.Visible)
            {
                this.To.Text = this.ToList.SelectedItem.Text;
            }

            if (this.To.Text.Length <= 0)
            {
                // recipient is required field
                this.PageContext.AddLoadMessage(this.GetText("need_to"), MessageTypes.warning);
                return;
            }

            // subject is required
            if (this.PmSubjectTextBox.Text.Trim().Length <= 0)
            {
                this.PageContext.AddLoadMessage(this.GetText("need_subject"), MessageTypes.warning);
                return;
            }

            // message is required
            if (this.editor.Text.Trim().Length <= 0)
            {
                this.PageContext.AddLoadMessage(this.GetText("need_message"), MessageTypes.warning);
                return;
            }

            if (this.ToList.SelectedItem is { Value: "0" })
            {
                // administrator is sending PMs to all users
                var body = HtmlHelper.StripHtml(BBCodeHelper.EncodeCodeBlocks(this.editor.Text));
                var messageFlags = new MessageFlags
                {
                    IsHtml = this.editor.UsesHTML,
                    IsBBCode = this.editor.UsesBBCode
                };

                // test user's PM count
                if (!this.VerifyMessageAllowed(1, body))
                {
                    return;
                }

                this.GetRepository<PMessage>().SendMessage(
                    this.PageContext.PageUserID,
                    0,
                    this.PmSubjectTextBox.Text,
                    body,
                    messageFlags.BitValue,
                    replyTo);

                // redirect to outbox (sent items), not control panel
                this.Get<LinkBuilder>().Redirect(ForumPages.MyMessages, "v={0}", "out");
            }
            else
            {
                // remove all abundant whitespaces and separators
                var rx = new Regex(@";(\s|;)*;");
                this.To.Text = rx.Replace(this.To.Text, ";");

                if (this.To.Text.StartsWith(";"))
                {
                    this.To.Text = this.To.Text.Substring(1);
                }

                if (this.To.Text.EndsWith(";"))
                {
                    this.To.Text = this.To.Text.Substring(0, this.To.Text.Length - 1);
                }

                rx = new Regex(@"\s*;\s*");
                this.To.Text = rx.Replace(this.To.Text, ";");

                // list of recipients
                var recipients = new List<string>(this.To.Text.Trim().Split(';'));

                if (recipients.Count > this.PageContext.BoardSettings.PrivateMessageMaxRecipients
                    && !this.PageContext.IsAdmin && this.PageContext.BoardSettings.PrivateMessageMaxRecipients != 0)
                {
                    // to many recipients
                    this.PageContext.AddLoadMessage(
                        this.GetTextFormatted(
                            "TOO_MANY_RECIPIENTS",
                            this.PageContext.BoardSettings.PrivateMessageMaxRecipients),
                        MessageTypes.warning);

                    return;
                }

                if (!this.VerifyMessageAllowed(recipients.Count, this.editor.Text))
                {
                    return;
                }

                // list of recipient's ids
                var recipientIds = new List<int>();

                // get recipients' IDs
                foreach (var recipient in recipients)
                {
                    var user = this.Get<IUserDisplayName>().FindUserByName(recipient);

                    if (user == null)
                    {
                        this.PageContext.AddLoadMessage(
                            this.GetTextFormatted("NO_SUCH_USER", recipient),
                            MessageTypes.warning);
                        return;
                    }

                    if (user.UserFlags.IsGuest)
                    {
                        this.PageContext.AddLoadMessage(this.GetText("NOT_GUEST"), MessageTypes.danger);
                        return;
                    }

                    // get recipient's ID from the database
                    if (!recipientIds.Contains(user.ID))
                    {
                        recipientIds.Add(user.ID);
                    }

                    var count = this.GetRepository<PMessage>().UserMessageCount(user.ID);

                    // test receiving user's PM count
                    if (count.NumberTotal + 1
                        < count.Allowed || this.PageContext.IsAdmin
                                        || this.Get<IAspNetUsersHelper>().GetBoardUser(user.ID, this.PageContext.PageBoardID).Item4.IsAdmin > 0)
                    {
                        continue;
                    }

                    // recipient has full PM box
                    this.PageContext.AddLoadMessage(
                        this.GetTextFormatted("RECIPIENTS_PMBOX_FULL", recipient),
                        MessageTypes.danger);
                    return;
                }

                // send PM to all recipients
                recipientIds.ForEach(
                    userId =>

                        {
                            var body = this.editor.Text;

                            var messageFlags = new MessageFlags
                            {
                                IsHtml = this.editor.UsesHTML,
                                IsBBCode = this.editor.UsesBBCode
                            };

                            this.GetRepository<PMessage>().SendMessage(
                                this.PageContext.PageUserID,
                                userId,
                                this.PmSubjectTextBox.Text,
                                body,
                                messageFlags.BitValue,
                                replyTo);

                            // reset lazy data as he should be informed at once
                            this.Get<IDataCache>().Remove(string.Format(Constants.Cache.ActiveUserLazyData, userId));

                            if (this.PageContext.BoardSettings.AllowPMEmailNotification)
                            {
                                this.Get<ISendNotification>().ToPrivateMessageRecipient(
                                    userId,
                                    this.PmSubjectTextBox.Text.Trim());
                            }
                        });

                // redirect to outbox (sent items), not control panel
                this.Get<LinkBuilder>().Redirect(ForumPages.MyMessages, "v={0}", "out");
            }
        }

        /// <summary>
        /// Verifies the message allowed.
        /// </summary>
        /// <param name="count">The recipients count.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Returns if the user is allowed to send a message or not
        /// </returns>
        private bool VerifyMessageAllowed(int count, string message)
        {
            // Check if SPAM Message first...
            if (!this.PageContext.IsAdmin && !this.PageContext.ForumModeratorAccess)
            {
                // Check content for spam
                if (this.Get<ISpamCheck>().CheckPostForSpam(
                    this.PageContext.IsGuest ? "Guest" : this.PageContext.User.DisplayOrUserName(),
                    this.PageContext.Get<HttpRequestBase>().GetUserRealIPAddress(),
                    message,
                    this.PageContext.MembershipUser.Email,
                    out var spamResult))
                {
                    var description =
                        $@"Spam Check detected possible SPAM ({spamResult}) Original message: [{message}]
                       posted by User: {(this.PageContext.IsGuest ? "Guest" : this.PageContext.User.DisplayOrUserName())}";

                    switch (this.PageContext.BoardSettings.SpamPostHandling)
                    {
                        case SpamPostHandling.DoNothing:
                            this.Logger.SpamMessageDetected(
                                this.PageContext.PageUserID,
                                description);
                            break;
                        case SpamPostHandling.FlagMessageUnapproved:
                            this.Logger.SpamMessageDetected(
                                this.PageContext.PageUserID,
                                $"{description}, it was flagged as unapproved post");
                            break;
                        case SpamPostHandling.RejectMessage:
                            this.Logger.SpamMessageDetected(
                                this.PageContext.PageUserID,
                                $"{description}, post was rejected");

                            this.PageContext.AddLoadMessage(this.GetText("SPAM_MESSAGE"), MessageTypes.danger);

                            break;
                        case SpamPostHandling.DeleteBanUser:
                            this.Logger.SpamMessageDetected(
                                this.PageContext.PageUserID,
                                $"{description}, user was deleted and bannded");

                            this.Get<IAspNetUsersHelper>().DeleteAndBanUser(
                                this.PageContext.PageUserID,
                                this.PageContext.MembershipUser,
                                this.PageContext.User.IP);

                            break;
                    }

                    return false;
                }
            }

            ///////////////////////////////

            // test sending user's PM count
            // get user's name
            var countInfo = this.GetRepository<PMessage>().UserMessageCount(this.PageContext.PageUserID);

            if (countInfo.NumberTotal + count <= countInfo.Allowed
                || this.PageContext.IsAdmin)
            {
                return true;
            }

            // user has full PM box
            this.PageContext.AddLoadMessage(
                this.GetTextFormatted("OWN_PMBOX_FULL", countInfo.Allowed),
                MessageTypes.danger);

            return false;
        }

        #endregion
    }
}