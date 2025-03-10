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
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.UI.WebControls;

    using YAF.Core.BasePages;
    using YAF.Core.Helpers;
    using YAF.Core.Model;
    using YAF.Core.Services;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Web.Extensions;

    #endregion

    /// <summary>
    /// The Admin Edit Admin Page Access Page
    /// </summary>
    public partial class PageAccessEdit : AdminPage
    {
        /// <summary>
        ///   Gets CurrentUserID.
        /// </summary>
        protected int CurrentUserID =>
            this.Get<LinkBuilder>().StringToIntOrRedirect(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("u"));

        #region Methods

        /// <summary>
        /// Cancels the click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void CancelClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            // get back to access admin list
            this.Get<LinkBuilder>().Redirect(ForumPages.Admin_PageAccessList);
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

            // current page label (no link)
            this.PageLinks.AddLink(this.GetText("ADMIN_PAGEACCESSEDIT", "TITLE"), string.Empty);
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (this.IsPostBack)
            {
                return;
            }

            this.BindData();
        }

        /// <summary>
        /// Handles the Click event of the Save control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void SaveClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.AccessList.Items.Cast<RepeaterItem>().ForEach(
                ri =>
                {
                    var readAccess = ri.FindControlAs<CheckBox>("ReadAccess").Checked;
                    var pageName = ri.FindControlAs<Label>("PageName").Text.Trim();

                    if (readAccess || string.Equals(
                        "Admin_Admin",
                        pageName,
                        StringComparison.InvariantCultureIgnoreCase))
                    {
                        // save it
                        this.GetRepository<AdminPageUserAccess>().Save(this.CurrentUserID, pageName);
                    }
                    else
                    {
                        this.GetRepository<AdminPageUserAccess>().Delete(this.CurrentUserID, pageName);
                    }
                });

            this.Get<IDataCache>().Remove(string.Format(Constants.Cache.AdminPageAccess, this.CurrentUserID));

            this.Get<LinkBuilder>().Redirect(ForumPages.Admin_PageAccessList);
        }

        /// <summary>
        /// Grants all click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void GrantAllClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            // save permissions to table -  checked only
            this.AccessList.Items.Cast<RepeaterItem>().ForEach(
                ri => this.GetRepository<AdminPageUserAccess>().Save(
                    this.CurrentUserID,
                    ri.FindControlAs<Label>("PageName").Text.Trim()));

            this.Get<IDataCache>().Remove(string.Format(Constants.Cache.AdminPageAccess, this.CurrentUserID));

            this.Get<LinkBuilder>().Redirect(ForumPages.Admin_PageAccessList);
        }

        /// <summary>
        /// The RevokeAll _Click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void RevokeAllClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.AccessList.Items.Cast<RepeaterItem>().ForEach(
                ri =>
                {
                    var pageName = ri.FindControlAs<Label>("PageName").Text.Trim();

                    // save it - admin index should be always available
                    if (!string.Equals("Admin_Admin", pageName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.GetRepository<AdminPageUserAccess>().Delete(
                            this.CurrentUserID,
                            ri.FindControlAs<Label>("PageName").Text.Trim());
                    }
                });

            this.Get<IDataCache>().Remove(string.Format(Constants.Cache.AdminPageAccess, this.CurrentUserID));

            this.Get<LinkBuilder>().Redirect(ForumPages.Admin_PageAccessList);
        }

        /// <summary>
        /// The PollGroup item command.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void AccessList_OnItemDataBound([NotNull] object source, [NotNull] RepeaterItemEventArgs e)
        {
            var item = e.Item;
            if (item.ItemType != ListItemType.Item && item.ItemType != ListItemType.AlternatingItem)
            {
                return;
            }

            var row = (AdminPageUserAccess)e.Item.DataItem;

            var pageName = item.FindControlRecursiveAs<Label>("PageName");
            var readAccess = item.FindControlRecursiveAs<CheckBox>("ReadAccess");
            pageName.Text = row.PageName;
            readAccess.Checked = row.ReadAccess;
        }

        /// <summary>
        /// The bind data.
        /// </summary>
        private void BindData()
        {
            var found = false;

            // Load the page access list.
            var dt = this.GetRepository<AdminPageUserAccess>().List(
                this.CurrentUserID);

            // Get admin pages by page prefixes.
            var listPages = Enum.GetNames(typeof(ForumPages)).Where(e => e.StartsWith("Admin_"));

            // Initialize list with a helper class.
            var pagesAll = new List<AdminPageUserAccess>();

            // Protected host-admin pages
            var hostPages = new[]
            {
                "Admin_Boards", "Admin_HostSettings", "Admin_PageAccessList", "Admin_PageAccessEdit"
            };

            // Iterate thru all admin pages
            listPages.ToList().ForEach(
                listPage =>
                {
                    if (dt != null && dt.Any(
                        a => a.PageName == listPage &&
                              hostPages.All(s => s != a.PageName)))
                    {
                        found = true;
                        pagesAll.Add(
                            new AdminPageUserAccess
                            {
                                UserID = this.CurrentUserID
                                    .ToType<int>(),
                                PageName = listPage,
                                ReadAccess = true
                            });
                    }

                    // If it doesn't contain page for the user add it.
                    if (!found && hostPages.All(s => s != listPage))
                    {
                        pagesAll.Add(
                            new AdminPageUserAccess
                            {
                                UserID = this.CurrentUserID
                                    .ToType<int>(),
                                PageName = listPage,
                                ReadAccess = false
                            });
                    }

                    // Reset flag in the end of the outer loop
                    found = false;
                });

            this.IconHeader.Text =
                $"{this.GetText("ADMIN_PAGEACCESSEDIT", "HEADER")}: <strong>{this.HtmlEncode(this.Get<IUserDisplayName>().GetNameById(this.CurrentUserID))}</strong>";

            // get admin pages list with access flags.
            this.AccessList.DataSource = pagesAll;

            this.DataBind();
        }

        #endregion
    }
}