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
    using System.Text;
    using System.Web.UI.WebControls;

    using YAF.Configuration;
    using YAF.Core.BasePages;
    using YAF.Core.Utilities;
    using YAF.Types;
    using YAF.Types.Extensions;
    using YAF.Types.Extensions.Data;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Data;
    using YAF.Web.Extensions;

    #endregion

    /// <summary>
    /// The Admin Database Maintenance Page
    /// </summary>
    public partial class ReIndex : AdminPage
    {
        #region Methods

        /// <summary>
        /// Registers the needed Java Scripts
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnPreRender([NotNull] EventArgs e)
        {
            this.PageContext.PageElements.RegisterJsBlockStartup(
                "BlockUiFunctionJs",
                JavaScriptBlocks.BlockUiFunctionJs(
                    "DeleteForumMessage"));

            base.OnPreRender(e);
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

            // Check and see if it should make panels enable or not
            this.PanelReindex.Visible = true;
            this.PanelShrink.Visible = true;
            this.PanelRecoveryMode.Visible = true;
            this.PanelGetStats.Visible = true;

            this.Shrink.ReturnConfirmText = this.GetText("ADMIN_REINDEX", "CONFIRM_SHRINK");
            this.Shrink.ReturnConfirmEvent = "blockUIMessage";

            this.Reindex.ReturnConfirmText = this.GetText("ADMIN_REINDEX", "CONFIRM_REINDEX");
            this.Reindex.ReturnConfirmEvent = "blockUIMessage";

            this.RecoveryMode.ReturnConfirmText = this.GetText("ADMIN_REINDEX", "CONFIRM_RECOVERY");

            this.RadioButtonList1.Items.Add(new ListItem(this.GetText("ADMIN_REINDEX", "RECOVERY1")));
            this.RadioButtonList1.Items.Add(new ListItem(this.GetText("ADMIN_REINDEX", "RECOVERY2")));
            this.RadioButtonList1.Items.Add(new ListItem(this.GetText("ADMIN_REINDEX", "RECOVERY3")));

            this.RadioButtonList1.SelectedIndex = 0;

            this.BindData();
        }

        /// <summary>
        /// Creates page links for this page.
        /// </summary>
        protected override void CreatePageLinks()
        {
            this.PageLinks.AddRoot();
            this.PageLinks.AddAdminIndex();
            this.PageLinks.AddLink(this.GetText("ADMIN_REINDEX", "TITLE"), string.Empty);
        }

        /// <summary>
        /// Gets the stats click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void GetStatsClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            try
            {
                this.txtIndexStatistics.Text = this.Get<IDbAccess>().GetDatabaseFragmentationInfo();
            }
            catch (Exception ex)
            {
                this.txtIndexStatistics.Text = $"Failure: {ex}";
            }
        }

        /// <summary>
        /// Sets the Recovery mode
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void RecoveryModeClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            var recoveryMode = this.RadioButtonList1.SelectedIndex switch
            {
                0 => "FULL",
                1 => "SIMPLE",
                2 => "BULK_LOGGED",
                _ => string.Empty
            };

            const string result = "Done";

            var stats = this.txtIndexStatistics.Text = this.Get<IDbAccess>().ChangeRecoveryMode(recoveryMode);

            this.txtIndexStatistics.Text = stats.IsSet() ? stats : result;
        }

        /// <summary>
        /// Re-indexing the Database
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ReindexClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            const string result = "Done";

            var stats = this.Get<IDbAccess>().ReIndexDatabase(Config.DatabaseObjectQualifier);

            this.txtIndexStatistics.Text = stats.IsSet() ? stats : result;
        }

        /// <summary>
        /// Mod By Touradg (herman_herman) 2009/10/19
        /// Shrinking Database
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ShrinkClick([NotNull] object sender, [NotNull] EventArgs e)
        {
            try
            {
                var result = new StringBuilder();

                result.Append(this.Get<IDbAccess>().ShrinkDatabase());

                result.Append(" ");

                result.AppendLine(this.GetTextFormatted(
                    "INDEX_SHRINK",
                    this.Get<IDbAccess>().GetDatabaseSize()));

                result.Append(" ");

                this.txtIndexStatistics.Text = result.ToString();
            }
            catch (Exception error)
            {
                this.txtIndexStatistics.Text += this.GetTextFormatted("INDEX_STATS_FAIL", error.Message);
            }
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
        private void BindData()
        {
            this.DataBind();
        }

        #endregion
    }
}