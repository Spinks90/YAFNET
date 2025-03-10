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
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Web;
    using System.Web.UI.HtmlControls;
    using System.Web.UI.WebControls;

    using YAF.Configuration;
    using YAF.Core.BasePages;
    using YAF.Core.Extensions;
    using YAF.Core.Model;
    using YAF.Core.Services;
    using YAF.Core.Utilities;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.EventProxies;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Events;
    using YAF.Types.Models;
    using YAF.Web.Extensions;

    #endregion

    /// <summary>
    /// The Edit User Album Images Page.
    /// </summary>
    public partial class EditAlbumImages : ForumPageRegistered
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EditAlbumImages"/> class.
        /// </summary>
        public EditAlbumImages()
            : base("EDIT_ALBUMIMAGES")
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// The back button click event handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Back_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (this.List.Items.Count > 0)
            {
                var albumId = this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("a");

                // This is an existing album
                if (albumId.IsSet() && !albumId.Contains("new"))
                {
                    this.Get<LinkBuilder>().Redirect(
                        ForumPages.Album,
                        "u={0}&a={1}",
                        this.PageContext.PageUserID.ToString(),
                        albumId);
                }
                else
                {
                    // simply redirect to albums page
                    this.Get<LinkBuilder>().Redirect(ForumPages.Albums, "u={0}", this.PageContext.PageUserID);
                }
            }
            else
            {
                this.Get<LinkBuilder>().Redirect(ForumPages.Albums, "u={0}", this.PageContext.PageUserID);
            }
        }

        /// <summary>
        /// Deletes the album and all the images in it.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void DeleteAlbum_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            this.DeleteAlbum();
        }

        /// <summary>
        /// The repeater Item command event responsible for handling deletion of uploaded files.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void List_ItemCommand([NotNull] object source, [NotNull] RepeaterCommandEventArgs e)
        {
            switch (e.CommandName)
            {
                case "delete":
                    var path = this.Get<HttpRequestBase>().MapPath(
                        string.Concat(BaseUrlBuilder.ServerFileRoot, this.Get<BoardFolders>().Uploads));

                    this.Get<IAlbum>().AlbumImageDelete(
                        path,
                        null,
                        this.PageContext.PageUserID,
                        e.CommandArgument.ToType<int>());

                    // If the user is trying to edit an existing album, initialize the repeater.
                    this.BindVariousControls(
                        this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("a").Equals("new"));

                    var data = this.GetRepository<User>().MaxAlbumData(
                        this.PageContext.PageUserID,
                        this.PageContext.PageBoardID);

                    var usrAlbumImagesAllowed = (int)data.UserAlbumImages;

                    // Check if user album is empty
                    if (usrAlbumImagesAllowed == 0)
                    {
                        this.DeleteAlbum();
                    }

                    // Has the user uploaded maximum number of images?   
                    // vzrus: changed for DB check The default number of album images is 0. In the case albums are disabled.
                    if (usrAlbumImagesAllowed > 0)
                    {
                        this.UploadHolder.Visible = !(this.List.Items.Count >= usrAlbumImagesAllowed);

                        this.imagesInfo.Text = this.GetTextFormatted(
                            "IMAGES_INFO",
                            this.List.Items.Count,
                            usrAlbumImagesAllowed,
                            this.PageContext.BoardSettings.AlbumImagesSizeMax / 1024);
                    }
                    else
                    {
                        this.UploadHolder.Visible = false;
                    }

                    break;
            }
        }

        /// <summary>
        /// the page load event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Page_Load([NotNull] object sender, [NotNull] EventArgs e)
        {
            if (!this.PageContext.BoardSettings.EnableAlbum)
            {
                this.Get<LinkBuilder>().AccessDenied();
            }

            this.PageContext.PageElements.RegisterJsBlockStartup(
                nameof(JavaScriptBlocks.FormValidatorJs),
                JavaScriptBlocks.FormValidatorJs(this.Upload.ClientID));

            if (this.IsPostBack)
            {
                return;
            }

            var data = this.GetRepository<User>().MaxAlbumData(
                this.PageContext.PageUserID,
                this.PageContext.PageBoardID);

            var usrAlbumsAllowed = (int)data.UserAlbumImages;

            var albumSize = this.GetRepository<UserAlbum>().CountUserAlbum(this.PageContext.PageUserID);

            switch (this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("a"))
            {
                // A new album is being created. check the permissions.
                case "new":

                    // Has the user created maximum number of albums?
                    if (usrAlbumsAllowed > 0 && albumSize >= usrAlbumsAllowed)
                    {
                        // Albums count. If we reached limit then we go to info page.
                        this.Get<LinkBuilder>().RedirectInfoPage(InfoMessage.AccessDenied);
                    }

                    this.UpdateTitle.Visible = false;

                    this.Header.LocalizedTag = "TITLE_NEW";

                    break;
            }

            this.BindData();

            var usrAlbumImagesAllowed = (int)data.UserAlbumImages;

            // Has the user uploaded maximum number of images?   
            // vzrus: changed for DB check The default number of album images is 0. In the case albums are disabled.
            if (usrAlbumImagesAllowed > 0)
            {
                this.UploadHolder.Visible = !(this.List.Items.Count >= usrAlbumImagesAllowed);

                this.imagesInfo.Text = this.GetTextFormatted(
                    "IMAGES_INFO",
                    this.List.Items.Count,
                    usrAlbumImagesAllowed,
                    this.PageContext.BoardSettings.AlbumImagesSizeMax / 1024);
            }
            else
            {
                this.UploadHolder.Visible = false;
            }
        }

        /// <summary>
        /// Create the Page links.
        /// </summary>
        protected override void CreatePageLinks()
        {
            var displayName = this.PageContext.User.DisplayOrUserName();

            // Add the page links.
            this.PageLinks.AddRoot();
            this.PageLinks.AddUser(this.PageContext.PageUserID, displayName);
            this.PageLinks.AddLink(
                this.GetText("ALBUMS"),
                this.Get<LinkBuilder>().GetLink(ForumPages.Albums, "u={0}", this.PageContext.PageUserID));
            this.PageLinks.AddLink(this.GetText("TITLE"), string.Empty);
        }

        /// <summary>
        /// Update the album title.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void UpdateTitle_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            var albumId = this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAsInt("a");
            this.txtTitle.Text = HttpUtility.HtmlEncode(this.txtTitle.Text);

            this.GetRepository<UserAlbum>().UpdateTitle(
                albumId.Value,
                this.txtTitle.Text);

            this.Get<LinkBuilder>().Redirect(ForumPages.EditAlbumImages, "a={0}", albumId.Value);
        }

        /// <summary>
        /// The Upload button click event handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void Upload_Click([NotNull] object sender, [NotNull] EventArgs e)
        {
            try
            {
                if (this.CheckValidFile(this.File))
                {
                    this.SaveAttachment(this.File);
                }

                this.BindData();

                var data = this.GetRepository<User>().MaxAlbumData(
                    this.PageContext.PageUserID,
                    this.PageContext.PageBoardID);

                var usrAlbumImagesAllowed = (int)data.UserAlbumImages;

                // Has the user uploaded maximum number of images?   
                // vzrus: changed for DB check The default number of album images is 0. In the case albums are disabled.
                if (usrAlbumImagesAllowed > 0)
                {
                    this.UploadHolder.Visible = !(this.List.Items.Count >= usrAlbumImagesAllowed);

                    this.imagesInfo.Text = this.GetTextFormatted(
                        "IMAGES_INFO",
                        this.List.Items.Count,
                        usrAlbumImagesAllowed,
                        this.PageContext.BoardSettings.AlbumImagesSizeMax / 1024);
                }
                else
                {
                    this.UploadHolder.Visible = false;
                }
            }
            catch (Exception x)
            {
                if (x.GetType() != typeof(ThreadAbortException))
                {
                    this.Logger.Log(this.PageContext.PageUserID, this, x);
                    this.PageContext.AddLoadMessage(x.Message, MessageTypes.danger);
                }
            }
        }

        /// <summary>
        /// Initializes the repeater control and the visibilities of form elements.
        /// </summary>
        private void BindData()
        {
            // If the user is trying to edit an existing album, initialize the repeater.
            this.BindVariousControls(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("a").Equals("new"));
        }

        /// <summary>
        /// Binds the repeater and title controls and the visibilities of form elements.
        /// </summary>
        /// <param name="isNewAlbum">if set to <c>true</c> [is new album].</param>
        private void BindVariousControls(bool isNewAlbum)
        {
            this.Delete.Visible = !isNewAlbum;

            if (!isNewAlbum)
            {
                this.txtTitle.Text = this.GetRepository<UserAlbum>()
                    .GetTitle(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<int>("a"));

                var albumList = this.GetRepository<UserAlbumImage>()
                    .List(this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<int>("a"));
                this.List.DataSource = albumList;
                this.List.Visible = albumList.Any();
            }

            this.DataBind();
        }

        /// <summary>
        /// Check to see if the user is trying to upload a valid file.
        /// </summary>
        /// <param name="uploadedFile">
        /// the uploaded file.
        /// </param>
        /// <returns>
        /// true if file is valid for uploading. otherwise false.
        /// </returns>
        private bool CheckValidFile([NotNull] HtmlInputFile uploadedFile)
        {
            var filePath = uploadedFile.PostedFile.FileName.Trim();

            if (filePath.IsNotSet() || uploadedFile.PostedFile.ContentLength == 0)
            {
                return false;
            }

            if (uploadedFile.PostedFile.ContentType.ToLower().Contains("text"))
            {
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLower();

            // remove the "period"
            extension = extension.Replace(".", string.Empty);
            string[] imageExtensions = { "jpg", "gif", "png", "bmp" };

            // If we don't get a match from the db, then the extension is not allowed
            // also, check to see an image is being uploaded.
            if (Array.IndexOf(imageExtensions, extension) != -1)
            {
                return true;
            }

            this.PageContext.AddLoadMessage(this.GetTextFormatted("FILEERROR", extension), MessageTypes.warning);
            return false;
        }

        /// <summary>
        /// Save the attached file both physically and in the Database
        /// </summary>
        /// <param name="file">the file.</param>
        /// <exception cref="Exception">Album Image File is too big</exception>
        private void SaveAttachment([NotNull] HtmlInputFile file)
        {
            if (file.PostedFile == null || file.PostedFile.FileName.Trim().Length == 0
                                        || file.PostedFile.ContentLength == 0)
            {
                return;
            }

            var path = this.Get<HttpRequestBase>()
                .MapPath(string.Concat(BaseUrlBuilder.ServerFileRoot, this.Get<BoardFolders>().Uploads));

            // check if Uploads folder exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var filename = file.PostedFile.FileName;

            var pos = filename.LastIndexOfAny(new[] { '/', '\\' });
            if (pos >= 0)
            {
                filename = filename.Substring(pos + 1);
            }

            // filename can be only 255 characters long (due to table column)
            if (filename.Length > 255)
            {
                filename = filename.Substring(filename.Length - 255);
            }

            // verify the size of the attachment
            if (this.PageContext.BoardSettings.AlbumImagesSizeMax > 0
                && file.PostedFile.ContentLength > this.PageContext.BoardSettings.AlbumImagesSizeMax)
            {
                this.PageContext.AddLoadMessage(this.GetText("ERROR_TOOBIG"), MessageTypes.danger);
                return;
            }

            // vzrus: the checks here are useless but in a case...
            var data = this.GetRepository<User>().MaxAlbumData(
                this.PageContext.PageUserID,
                this.PageContext.PageBoardID);

            var usrAlbumsAllowed = (int)data.UserAlbum;
            var usrAlbumImagesAllowed = (int)data.UserAlbumImages;

            // if (!usrAlbums.HasValue || usrAlbums <= 0) return;
            if (this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("a") == "new")
            {
                var allStats = this.GetRepository<UserAlbum>().CountUserAlbum(this.PageContext.PageUserID);

                // Albums count. If we reached limit then we exit.
                if (allStats >= usrAlbumsAllowed)
                {
                    this.PageContext.AddLoadMessage(this.GetTextFormatted("ALBUMS_COUNT_LIMIT", usrAlbumImagesAllowed), MessageTypes.warning);
                    return;
                }

                var newAlbumId = this.GetRepository<UserAlbum>().Save(
                    this.PageContext.PageUserID,
                    this.txtTitle.Text,
                    null);

                file.PostedFile.SaveAs(
                    $"{path}/{this.PageContext.PageUserID}.{newAlbumId.ToString()}.{filename}.yafalbum");

                this.GetRepository<UserAlbumImage>().Save(
                    null,
                    newAlbumId,
                    null,
                    filename,
                    file.PostedFile.ContentLength,
                    file.PostedFile.ContentType);

                // clear the cache for this user to update albums|images stats...
                this.Get<IRaiseEvent>().Raise(new UpdateUserEvent(this.PageContext.PageUserID));

                this.Get<LinkBuilder>().Redirect(ForumPages.EditAlbumImages, "a={0}", newAlbumId);
            }
            else
            {
                // vzrus: the checks here are useless but in a case...
                var allStats = this.GetRepository<UserAlbumImage>().CountAlbumImages(
                    this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<int>("a"));

                // Images count. If we reached limit then we exit.
                if (allStats >= usrAlbumImagesAllowed)
                {
                    this.PageContext.AddLoadMessage(this.GetTextFormatted("IMAGES_COUNT_LIMIT", usrAlbumImagesAllowed), MessageTypes.warning);
                    return;
                }

                file.PostedFile.SaveAs(
                    $"{path}/{this.PageContext.PageUserID}.{this.Get<HttpRequestBase>().QueryString.GetFirstOrDefault("a")}.{filename}.yafalbum");
                this.GetRepository<UserAlbumImage>().Save(
                    null,
                    this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAs<int>("a"),
                    null,
                    filename,
                    file.PostedFile.ContentLength,
                    file.PostedFile.ContentType);
            }
        }

        /// <summary>
        /// Deletes the Entire Album
        /// </summary>
        private void DeleteAlbum()
        {
            var path = this.Get<HttpRequestBase>()
                .MapPath(string.Concat(BaseUrlBuilder.ServerFileRoot, this.Get<BoardFolders>().Uploads));

            this.Get<IAlbum>().AlbumImageDelete(
                path,
                this.Get<HttpRequestBase>().QueryString.GetFirstOrDefaultAsInt("a"),
                this.PageContext.PageUserID,
                null);

            // clear the cache for this user to update albums|images stats...
            this.Get<IRaiseEvent>().Raise(new UpdateUserEvent(this.PageContext.PageUserID));

            this.Get<LinkBuilder>().Redirect(ForumPages.Albums, "u={0}", this.PageContext.PageUserID);
        }

        #endregion
    }
}