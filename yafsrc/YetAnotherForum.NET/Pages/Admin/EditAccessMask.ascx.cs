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

namespace YAF.Pages.Admin
{
    #region Using

    using System;
    using System.Web;

    using YAF.Core.BasePages;
    using YAF.Core.Extensions;
    using YAF.Core.Helpers;
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
    /// Admin Page for Editing or Creating an Forum Access Mask
    /// </summary>
    public partial class EditAccessMask : AdminPage
    {
        #region Methods

        /// <summary>
        /// Cancel Edit and Return Back To Access Mask List Page.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void CancelClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            // get back to access masks administration
            this.Get<LinkBuilder>().Redirect(ForumPages.Admin_AccessMasks);
        }

        /// <summary>
        /// Creates navigation page links on top of forum (breadcrumbs).
        /// </summary>
        protected override void CreatePageLinks()
        {
            // beard index
            this.PageLinks.AddRoot();

            // administration index
            this.PageLinks.AddAdminIndex();

            this.PageLinks.AddLink(
                this.GetText("ADMIN_ACCESSMASKS", "TITLE"),
                this.Get<LinkBuilder>().GetLink(ForumPages.Admin_AccessMasks));

            // current page label (no link)
            this.PageLinks.AddLink(this.GetText("ADMIN_EDITACCESSMASKS", "TITLE"), string.Empty);
        }

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

            this.PageContext.PageElements.RegisterJsBlockStartup(
                nameof(JavaScriptBlocks.FormValidatorJs),
                JavaScriptBlocks.FormValidatorJs(this.Save.ClientID));

            // bind data
            this.BindData();
        }

        /// <summary>
        /// Saves The Access Mask
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void SaveClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            // retrieve access mask ID from parameter (if applicable)
            int? accessMaskId = null;

            if (this.Get<HttpRequestBase>().QueryString.Exists("i"))
            {
                accessMaskId = this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<int>("i");
            }

            if (!ValidationHelper.IsValidPosShort(this.SortOrder.Text.Trim()))
            {
                this.PageContext.AddLoadMessage(
                    this.GetText("ADMIN_EDITACCESSMASKS", "MSG_POSITIVE_SORT"),
                    MessageTypes.warning);
                return;
            }

            if (!short.TryParse(this.SortOrder.Text.Trim(), out var sortOrder))
            {
                this.PageContext.AddLoadMessage(
                    this.GetText("ADMIN_EDITACCESSMASKS", "MSG_NUMBER_SORT"),
                    MessageTypes.warning);
                return;
            }

            // save it
            this.GetRepository<AccessMask>()
                .Save(
                    accessMaskId,
                    this.Name.Text,
                    this.ReadAccess.Checked,
                    this.PostAccess.Checked,
                    this.ReplyAccess.Checked,
                    this.PriorityAccess.Checked,
                    this.PollAccess.Checked,
                    this.VoteAccess.Checked,
                    this.ModeratorAccess.Checked,
                    this.EditAccess.Checked,
                    this.DeleteAccess.Checked,
                    this.UploadAccess.Checked,
                    this.DownloadAccess.Checked,
                    sortOrder);

            // get back to access masks administration
            this.Get<LinkBuilder>().Redirect(ForumPages.Admin_AccessMasks);
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
        private void BindData()
        {
            if (this.Get<HttpRequestBase>().QueryString.Exists("i"))
            {
                var accessMask = this.GetRepository<AccessMask>()
                    .GetById(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<int>("i"));

                if (accessMask != null)
                {
                    // get access mask properties
                    this.Name.Text = accessMask.Name;
                    this.SortOrder.Text = accessMask.SortOrder.ToString();

                    // get flags
                    this.ReadAccess.Checked = accessMask.AccessFlags.ReadAccess;
                    this.PostAccess.Checked = accessMask.AccessFlags.PostAccess;
                    this.ReplyAccess.Checked = accessMask.AccessFlags.ReplyAccess;
                    this.PriorityAccess.Checked = accessMask.AccessFlags.PriorityAccess;
                    this.PollAccess.Checked = accessMask.AccessFlags.PollAccess;
                    this.VoteAccess.Checked = accessMask.AccessFlags.VoteAccess;
                    this.ModeratorAccess.Checked = accessMask.AccessFlags.ModeratorAccess;
                    this.EditAccess.Checked = accessMask.AccessFlags.EditAccess;
                    this.DeleteAccess.Checked = accessMask.AccessFlags.DeleteAccess;
                    this.UploadAccess.Checked = accessMask.AccessFlags.UploadAccess;
                    this.DownloadAccess.Checked = accessMask.AccessFlags.DownloadAccess;
                }
                else
                {
                    this.Get<LinkBuilder>().RedirectInfoPage(InfoMessage.Invalid);
                }
            }
            else
            {
                this.SortOrder.Text =
                    (this.GetRepository<AccessMask>().Count(x => x.BoardID == this.PageContext.PageBoardID) + 1)
                    .ToString();
            }

            this.DataBind();
        }

        #endregion
    }
}