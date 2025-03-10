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

namespace YAF.Modules
{
    #region Using

    using System;
    using System.Collections.Specialized;
    using System.Web;
    using System.Web.UI;

    using YAF.Configuration;
    using YAF.Core.Helpers;
    using YAF.Core.Utilities;
    using YAF.Types;
    using YAF.Types.Attributes;
    using YAF.Types.Interfaces;

    #endregion

    /// <summary>
    /// Automatic JavaScript Loading Module
    /// </summary>
    [Module("JavaScript Loading Module", "Ingo Herbote", 1)]
    public class ScriptsLoaderModule : SimpleBaseForumModule
    {
        #region Public Methods

        /// <summary>
        /// The initializes after page.
        /// </summary>
        public override void InitAfterPage()
        {
            this.CurrentForumPage.PreRender += this.CurrentForumPagePreRender;
            this.CurrentForumPage.Load += this.CurrentForumPageLoad;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Registers the jQuery script library.
        /// </summary>
        private void RegisterJQuery()
        {
            if (this.PageContext.PageElements.PageElementExists("jquery"))
            {
                return;
            }

            var registerJQuery = true;

            const string Key = "JQuery-Javascripts";

            // check to see if DotNetAge is around and has registered jQuery for us...
            if (HttpContext.Current.Items[Key] != null)
            {
                if (HttpContext.Current.Items[Key] is StringCollection collection && collection.Contains("jquery"))
                {
                    registerJQuery = false;
                }
            }
            else if (Config.IsDotNetNuke)
            {
                // latest version of DNN should register jQuery for us...
                registerJQuery = false;
            }

            if (registerJQuery)
            {
                this.PageContext.PageElements.AddScriptReference("jquery");
            }

            this.PageContext.PageElements.AddPageElement("jquery");
        }

        /// <summary>
        /// Handles the Load event of the ForumPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CurrentForumPageLoad([NotNull] object sender, [NotNull] EventArgs e)
        {
            // Load CSS First
            this.RegisterCssFiles(this.PageContext.BoardSettings.CdvVersion);
        }

        /// <summary>
        /// Handles the PreRender event of the CurrentForumPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void CurrentForumPagePreRender([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.RegisterJQuery();

            if (this.PageContext.Vars.ContainsKey("yafForumExtensions"))
            {
                return;
            }

            var version = this.PageContext.BoardSettings.CdvVersion;

            var forumJsName = Config.IsDotNetNuke ? "ForumExtensionsDnn" : "ForumExtensions";
            var adminForumJsName = Config.IsDotNetNuke ? "ForumAdminExtensionsDnn" : "ForumAdminExtensions";

            ScriptManager.ScriptResourceMapping.AddDefinition(
                "yafForumAdminExtensions",
                new ScriptResourceDefinition
                {
                    Path = BoardInfo.GetURLToScripts($"jquery.{adminForumJsName}.min.js?v={version}"),
                    DebugPath = BoardInfo.GetURLToScripts($"jquery.{adminForumJsName}.js?v={version}")
                });

            ScriptManager.ScriptResourceMapping.AddDefinition(
                "yafForumExtensions",
                new ScriptResourceDefinition
                {
                    Path = BoardInfo.GetURLToScripts($"jquery.{forumJsName}.min.js?v={version}"),
                    DebugPath = BoardInfo.GetURLToScripts($"jquery.{forumJsName}.js?v={version}")
                });

            ScriptManager.ScriptResourceMapping.AddDefinition(
                "FileUploadScript",
                new ScriptResourceDefinition
                {
                    Path = BoardInfo.GetURLToScripts("jquery.fileupload.comb.min.js"),
                    DebugPath = BoardInfo.GetURLToScripts("jquery.fileupload.comb.js")
                });

            this.PageContext.PageElements.AddScriptReference(
                this.PageContext.CurrentForumPage.IsAdminPage ? "yafForumAdminExtensions" : "yafForumExtensions");

            this.PageContext.Vars["yafForumExtensions"] = true;
        }

        /// <summary>
        /// Register the CSS Files in the header.
        /// </summary>
        /// <param name="version">
        /// The version.
        /// </param>
        private void RegisterCssFiles(int version)
        {
            var element = this.PageContext.CurrentForumPage.TopPageControl;

            element.Controls.Add(
                ControlHelper.MakeCssIncludeControl(
                    $"{this.Get<ITheme>().BuildThemePath("bootstrap-forum.min.css")}?v={version}"));

            // make the style sheet link controls.
            element.Controls.Add(
                ControlHelper.MakeCssIncludeControl(
                    BoardInfo.GetURLToContent(
                        this.PageContext.CurrentForumPage.IsAdminPage
                            ? $"forum-admin.min.css?v={version}"
                            : $"forum.min.css?v={version}")));
        }

        #endregion
    }
}