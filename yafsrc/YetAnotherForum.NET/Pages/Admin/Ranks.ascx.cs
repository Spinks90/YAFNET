/* Yet Another Forum.NET
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
    using System.Web.UI.WebControls;

    using YAF.Core.BasePages;
    using YAF.Core.Extensions;
    using YAF.Core.Services;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Models;
    using YAF.Web.Extensions;

    #endregion

    /// <summary>
    /// Admin Ranks Page
    /// </summary>
    public partial class Ranks : AdminPage
    {
        #region Methods

        /// <summary>
        /// Format string color.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <returns>
        /// Set values  are rendered green if true, and if not red
        /// </returns>
        protected string GetItemColorString(string item)
        {
            // show enabled flag red
            return item.IsSet() ? "badge bg-success" : "badge bg-danger";
        }

        /// <summary>
        /// Format access mask setting color formatting.
        /// </summary>
        /// <param name="enabled">
        /// The enabled.
        /// </param>
        /// <returns>
        /// Set access mask flags  are rendered green if true, and if not red
        /// </returns>
        protected string GetItemColor(bool enabled)
        {
            // show enabled flag red
            return enabled ? "badge bg-success" : "badge bg-danger";
        }

        /// <summary>
        /// Get a user friendly item name.
        /// </summary>
        /// <param name="enabled">
        /// The enabled.
        /// </param>
        /// <returns>
        /// Item Name.
        /// </returns>
        protected string GetItemName(bool enabled)
        {
            return enabled ? this.GetText("DEFAULT", "YES") : this.GetText("DEFAULT", "NO");
        }

        /// <summary>
        /// The bit set.
        /// </summary>
        /// <param name="flags">
        /// The flags.
        /// </param>
        /// <param name="bitmask">
        /// The bitmask.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        protected bool BitSet([NotNull] object flags, int bitmask)
        {
            var i = (int)flags;
            return (i & bitmask) != 0;
        }

        /// <summary>
        /// Is Rank Ladder?
        /// </summary>
        /// <param name="rank">
        /// The rank.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        protected string LadderInfo([NotNull] object rank)
        {
            var dr = (Rank)rank;

            var isLadder = dr.RankFlags.IsLadder;

            return isLadder
                       ? $"{this.GetItemName(true)} ({dr.MinPosts} {this.GetText("ADMIN_RANKS", "POSTS")})"
                       : this.GetItemName(false);
        }

        /// <summary>
        /// News the rank click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void NewRankClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.Get<LinkBuilder>().Redirect(ForumPages.Admin_EditRank);
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
        /// Creates page links for this page.
        /// </summary>
        protected override void CreatePageLinks()
        {
            this.PageLinks.AddRoot();
            this.PageLinks.AddAdminIndex();

            this.PageLinks.AddLink(this.GetText("ADMIN_RANKS", "TITLE"));
        }

        /// <summary>
        /// The rank list_ item command.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected void RankListItemCommand([NotNull] object source, [NotNull] RepeaterCommandEventArgs e)
        {
            switch (e.CommandName)
            {
                case "edit":
                    this.Get<LinkBuilder>().Redirect(ForumPages.Admin_EditRank, "r={0}", e.CommandArgument);
                    break;
                case "delete":
                    this.GetRepository<Rank>().DeleteById(e.CommandArgument.ToType<int>());
                    this.BindData();
                    break;
            }
        }

        /// <summary>
        /// The bind data.
        /// </summary>
        private void BindData()
        {
            this.RankList.DataSource = this.GetRepository<Rank>().GetByBoardId();
            this.DataBind();
        }

        #endregion
    }
}