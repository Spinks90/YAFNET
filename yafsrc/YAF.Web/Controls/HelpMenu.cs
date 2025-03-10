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
namespace YAF.Web.Controls
{
    #region Using

    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using System.Xml.Serialization;

    using YAF.Configuration;
    using YAF.Core.BaseControls;
    using YAF.Core.Services;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Objects;

    #endregion

    /// <summary>
    /// Renders the Help Menu on the Help Pages.
    /// </summary>
    public class HelpMenu : BasePanel
    {
        #region Methods

        /// <summary>
        /// The List with the Help Navigation Items
        /// </summary>
        private List<HelpNavigation> helpNavList = new();

        /// <summary>
        /// The render.
        /// </summary>
        /// <param name="writer">
        /// The writer.
        /// </param>
        protected override void Render([NotNull] HtmlTextWriter writer)
        {
            var serializer = new XmlSerializer(typeof(List<HelpNavigation>));

            var xmlFilePath = this.Get<HttpContextBase>().Server.MapPath(
                $"{BoardInfo.ForumServerFileRoot}Resources/HelpMenuList.xml");

            if (File.Exists(xmlFilePath))
            {
                var reader = new StreamReader(xmlFilePath);

                this.helpNavList = (List<HelpNavigation>)serializer.Deserialize(reader);

                reader.Close();
            }

            var html = new StringBuilder();
            var htmlDropDown = new StringBuilder();

            htmlDropDown.Append(@"<div class=""dropdown d-lg-none d-grid gap-2"">");

            htmlDropDown.Append(
                @"<button class=""btn btn-secondary dropdown-toggle"" type=""button"" id=""dropdownMenuButton"" data-bs-toggle=""dropdown"" aria-haspopup=""true"" aria-expanded=""false"">");

            htmlDropDown.AppendFormat(@"{0}</button>", this.GetText("HELP_INDEX", "INDEX"));

            htmlDropDown.Append(
                @"<div class=""dropdown-menu scrollable-dropdown"" aria-labelledby=""dropdownMenuButton"">");

            var faqPage = "index";

            if (this.Get<HttpRequestBase>().QueryString.Exists("faq"))
            {
                faqPage = this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("faq");
            }

            var selectedStyle = string.Empty;

            if (faqPage.Equals("index"))
            {
                selectedStyle = @"style=""color:red;""";
            }

            html.AppendFormat(
                @"<h6 class=""sidebar-heading d-flex justify-content-between align-items-center px-3 mt-4 mb-1 text-muted""><span class=""text-uppercase fw-bold""><a class=""text-secondary text-bold"" href=""{2}"" {3}>{0} / {1}</a></span></h6>",
                this.GetText("HELP_INDEX", "INDEX"),
                this.GetText("BTNSEARCH"),
                this.Get<LinkBuilder>().GetLink(ForumPages.Help, "faq=index"),
                selectedStyle);

            htmlDropDown.AppendFormat(
                @"<a href=""{1}"" class=""dropdown-item"">{0}</a>",
                this.GetText("BTNSEARCH"),
                this.Get<LinkBuilder>().GetLink(ForumPages.Help, "faq=index"));

            html.Append("<hr />");

            this.helpNavList.ForEach(
                category =>
                    {
                        html.AppendFormat(
                            @"<h6 class=""sidebar-heading d-flex justify-content-between align-items-center px-3 mt-4 py-3 mb-0 text-light bg-dark""><span>{0}</span></h6>",
                            this.GetText("HELP_INDEX", category.HelpCategory));

                        htmlDropDown.AppendFormat(
                            @"<h6 class=""dropdown-header"">{0}</h6>",
                            this.GetText("HELP_INDEX", category.HelpCategory));

                        html.Append(@"<ul class=""nav flex-column mb-2"">");

                        category.HelpPages.ForEach(
                            helpPage =>
                                {
                                    selectedStyle = string.Empty;

                                    if (helpPage.HelpPage.ToLower().Equals(faqPage))
                                    {
                                        selectedStyle = " text-info";
                                    }

                                    if (helpPage.HelpPage.Equals("REGISTRATION"))
                                    {
                                        if (this.PageContext.BoardSettings.DisableRegistrations || Config.IsAnyPortal)
                                        {
                                            return;
                                        }

                                        html.AppendFormat(
                                            @"<li class=""nav-item""><a href=""{0}"" title=""{1}"" class=""nav-link{2}"">{1}</a></li>",
                                            this.Get<LinkBuilder>().GetLink(
                                                ForumPages.Help,
                                                $"faq={helpPage.HelpPage.ToLower()}"),
                                            this.GetText("HELP_INDEX", $"{helpPage.HelpPage}TITLE"),
                                            selectedStyle);

                                        htmlDropDown.AppendFormat(
                                            @"<a href=""{0}"" class=""dropdown-item"">{1}</a>",
                                            this.Get<LinkBuilder>().GetLink(
                                                ForumPages.Help,
                                                $"faq={helpPage.HelpPage.ToLower()}"),
                                            this.GetText("HELP_INDEX", $"{helpPage.HelpPage}TITLE"));
                                    }
                                    else
                                    {
                                        html.AppendFormat(
                                            @"<li class=""nav-item""><a href=""{0}"" title=""{1}"" class=""nav-link{2}"">{1}</a></li>",
                                            this.Get<LinkBuilder>().GetLink(
                                                ForumPages.Help,
                                                $"faq={helpPage.HelpPage.ToLower()}"),
                                            this.GetText("HELP_INDEX", $"{helpPage.HelpPage}TITLE"),
                                            selectedStyle);

                                        htmlDropDown.AppendFormat(
                                            @"<a href=""{0}"" class=""dropdown-item"">{1}</a>",
                                            this.Get<LinkBuilder>().GetLink(
                                                ForumPages.Help,
                                                $"faq={helpPage.HelpPage.ToLower()}"),
                                            this.GetText("HELP_INDEX", $"{helpPage.HelpPage}TITLE"));
                                    }
                                });

                        html.Append(@"</ul>");
                    });

            htmlDropDown.Append(@"</div></div>");

            writer.BeginRender();
            writer.WriteLine(@"<div id=""container-fluid""><div class=""row no-gutters"">");

            // render the contents of the help menu....
            writer.WriteLine(
                @"<div class=""col-md-3 d-none d-lg-block bg-light sidebar""><div class=""sidebar-sticky"">");

            writer.Write(html.ToString());

            writer.WriteLine(@"</div></div>");

            // contents of the help pages...
            writer.WriteLine(@"<div class=""col flex-grow-1 ms-lg-3"">");

            // Write Mobile Drop down
            writer.WriteLine(htmlDropDown.ToString());

            this.RenderChildren(writer);

            writer.WriteLine("</div>");

            writer.WriteLine("</div></div>");

            writer.EndRender();
        }

        #endregion
    }
}