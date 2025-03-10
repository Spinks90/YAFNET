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

namespace YAF.Core.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Web;
    using System.Web.Configuration;

    using ServiceStack.OrmLite;

    using YAF.Configuration;
    using YAF.Core.Data;
    using YAF.Core.Extensions;
    using YAF.Core.Helpers;
    using YAF.Core.Model;
    using YAF.Core.Services.Import;
    using YAF.Core.Services.Migrations;
    using YAF.Core.Tasks;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.EventProxies;
    using YAF.Types.Extensions;
    using YAF.Types.Extensions.Data;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Data;
    using YAF.Types.Interfaces.Events;
    using YAF.Types.Models;
    using YAF.Types.Models.Identity;

    /// <summary>
    ///     The install upgrade service.
    /// </summary>
    public class InstallUpgradeService : IHaveServiceLocator
    {
        #region Constants

        /// <summary>
        ///     The BBCode extensions import xml file.
        /// </summary>
        private const string BbcodeImport = "BBCodeExtensions.xml";

        /// <summary>
        ///     The Spam Words list import xml file.
        /// </summary>
        private const string SpamWordsImport = "SpamWords.xml";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallUpgradeService"/> class.
        /// </summary>
        /// <param name="serviceLocator">
        /// The service locator.
        /// </param>
        /// <param name="raiseEvent">
        /// The raise Event.
        /// </param>
        /// <param name="access">
        /// The access.
        /// </param>
        public InstallUpgradeService(IServiceLocator serviceLocator, IRaiseEvent raiseEvent, IDbAccess access)
        {
            this.RaiseEvent = raiseEvent;
            this.DbAccess = access;
            this.ServiceLocator = serviceLocator;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets a value indicating whether this instance is forum installed.
        /// </summary>
        public bool IsForumInstalled
        {
            get
            {
                try
                {
                    var boards = this.GetRepository<Board>().GetAll();
                    return boards.Any();
                }
                catch
                {
                    // failure... no boards.
                    return false;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the raise event.
        /// </summary>
        public IRaiseEvent RaiseEvent { get; set; }

        /// <summary>
        /// Gets or sets the database access.
        /// </summary>
        /// <value>
        /// The database access.
        /// </value>
        public IDbAccess DbAccess { get; set; }

        /// <summary>
        ///     Gets or sets the service locator.
        /// </summary>
        public IServiceLocator ServiceLocator { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Initializes the forum.
        /// </summary>
        /// <param name="applicationId">
        /// The application Id.
        /// </param>
        /// <param name="forumName">
        /// The forum name.
        /// </param>
        /// <param name="culture">
        /// The culture.
        /// </param>
        /// <param name="forumEmail">
        /// The forum email.
        /// </param>
        /// <param name="forumLogo">
        /// The forum Logo.
        /// </param>
        /// <param name="forumBaseUrlMask">
        /// The forum base URL mask.
        /// </param>
        /// <param name="adminUserName">
        /// The admin user name.
        /// </param>
        /// <param name="adminEmail">
        /// The admin email.
        /// </param>
        /// <param name="adminProviderUserKey">
        /// The admin provider user key.
        /// </param>
        public void InitializeForum(
            [NotNull] Guid applicationId,
            [NotNull] string forumName,
            [NotNull] string culture,
            [CanBeNull] string forumEmail,
            [NotNull] string forumLogo,
            [NotNull] string forumBaseUrlMask,
            [NotNull] string adminUserName,
            [NotNull] string adminEmail,
            [NotNull] string adminProviderUserKey)
        {
            CodeContracts.VerifyNotNull(forumName);
            CodeContracts.VerifyNotNull(forumName);
            CodeContracts.VerifyNotNull(culture);
            CodeContracts.VerifyNotNull(forumLogo);
            CodeContracts.VerifyNotNull(forumBaseUrlMask);
            CodeContracts.VerifyNotNull(adminUserName);
            CodeContracts.VerifyNotNull(adminEmail);
            CodeContracts.VerifyNotNull(adminProviderUserKey);

            var cult = StaticDataHelper.Cultures();

            var languageFromCulture = cult
                .FirstOrDefault(c => c.CultureTag == culture);

            var langFile = languageFromCulture != null ? languageFromCulture.CultureFile : "english.xml";

            // -- initialize required 'registry' settings
            this.GetRepository<Registry>().Save("applicationid", applicationId.ToString());

            if (forumEmail.IsSet())
            {
                this.GetRepository<Registry>().Save("forumemail", forumEmail);
            }

            this.GetRepository<Registry>().Save("forumlogo", forumLogo);
            this.GetRepository<Registry>().Save("baseurlmask", forumBaseUrlMask);

            this.GetRepository<Board>().Create(
                forumName,
                forumEmail,
                culture,
                langFile,
                adminUserName,
                adminEmail,
                adminProviderUserKey,
                true,
                Config.CreateDistinctRoles && Config.IsAnyPortal ? "YAF " : string.Empty);

            this.AddOrUpdateExtensions();
        }

        /// <summary>
        /// Tests database connection. Can probably be moved to DB class.
        /// </summary>
        /// <param name="exceptionMessage">
        /// The exception message.
        /// </param>
        /// <returns>
        /// The test database connection.
        /// </returns>
        public bool TestDatabaseConnection([NotNull] out string exceptionMessage)
        {
            return this.DbAccess.TestConnection(out exceptionMessage);
        }

        /// <summary>
        /// Initialize Or Upgrade the Database
        /// </summary>
        /// <param name="updateExtensions">
        /// update Extensions ?!.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool InitializeOrUpgradeDatabase(bool updateExtensions)
        {
            var isForumInstalled = this.IsForumInstalled;

            this.CreateOrUpdateTables();

            if (!isForumInstalled)
            {
                this.ExecuteInstallScripts();
            }

            var prevVersion = this.GetRepository<Registry>().GetDbVersion();

            this.GetRepository<Registry>().Save("version", BoardInfo.AppVersion.ToString());
            this.GetRepository<Registry>().Save("versionname", BoardInfo.AppVersionName);

            if (isForumInstalled)
            {
                if (prevVersion < 80)
                {
                    if (!Config.IsDotNetNuke)
                    {
                        this.MigrateConfig();
                    }

                    // Migrate File Extensions
                    var extensions = this.GetRepository<FileExtension>()
                        .Get(x => x.BoardId == this.Get<BoardSettings>().BoardID);

                    this.GetRepository<Registry>().Save(
                        "allowedfileextensions",
                        extensions.Select(x => x.Extension).ToDelimitedString(","));

                    this.Get<V80_Migration>().MigrateDatabase(this.DbAccess);

                    // Upgrade to ASPNET Identity
                    if (!Config.IsDotNetNuke)
                    {
                        this.DbAccess.Information.IdentityUpgradeScripts.ForEach(this.ExecuteScript);
                    }

                    this.Get<ITaskModuleManager>().StartTask(MigrateAttachmentsTask.TaskName, () => new MigrateAttachmentsTask());

                    while (this.Get<ITaskModuleManager>().IsTaskRunning(MigrateAttachmentsTask.TaskName))
                    {
                        Thread.Sleep(100);
                    }

                    // Delete old registry Settings
                    this.GetRepository<Registry>().DeleteLegacy();

                    // update default points from 0 to 1
                    this.GetRepository<User>().UpdateOnly(() => new User { Points = 1 }, u => u.Points == 0);
                }

                if (prevVersion < 30 || updateExtensions)
                {
                    this.AddOrUpdateExtensions();
                }

                if (prevVersion < 42)
                {
                    // un-html encode all topic subject names...
                    this.GetRepository<Topic>().UnEncodeAllTopicsSubjects(HttpUtility.HtmlDecode);
                }

                // initialize search index
                if (this.Get<BoardSettings>().LastSearchIndexUpdated.IsNotSet())
                {
                    this.GetRepository<Registry>().Save("forceupdatesearchindex", "1");
                }

                // Check if BaseUrlMask is set and if not automatically write it
                if (this.Get<BoardSettings>().BaseUrlMask.IsNotSet())
                {
                    this.GetRepository<Registry>().Save("baseurlmask", BaseUrlBuilder.GetBaseUrlFromVariables());
                }

                this.GetRepository<Registry>().Save("cdvversion", this.Get<BoardSettings>().CdvVersion++);

                this.Get<IDataCache>().Remove(Constants.Cache.Version);
            }

            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes the install scripts.
        /// </summary>
        private void ExecuteInstallScripts()
        {
            if (!Config.IsDotNetNuke)
            {
                // Install Membership Scripts
                this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<AspNetUsers>());
                this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<AspNetRoles>());
                this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<AspNetUserClaims>());
                this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<AspNetUserLogins>());
                this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<AspNetUserRoles>());
            }

            // Run other
            this.DbAccess.Execute(dbCommand => this.DbAccess.Information.CreateViews(this.DbAccess, dbCommand));

            this.DbAccess.Execute(dbCommand => this.DbAccess.Information.CreateIndexViews(this.DbAccess, dbCommand));
        }

        /// <summary>
        /// Crate Tables and Update Tables
        /// </summary>
        private void CreateOrUpdateTables()
        {
            this.CreateTablesIfNotExists();
        }

        /// <summary>
        /// Create missing tables
        /// </summary>
        private void CreateTablesIfNotExists()
        {
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Board>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Rank>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<User>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Category>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Forum>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Topic>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Message>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Thanks>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Buddy>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Types.Models.FavoriteTopic>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<UserAlbum>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<UserAlbumImage>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Active>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<ActiveAccess>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Activity>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<AdminPageUserAccess>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Group>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<BannedIP>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<BannedName>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<BannedEmail>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<CheckEmail>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Poll>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Choice>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<PollVote>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<AccessMask>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<ForumAccess>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<MessageHistory>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<MessageReported>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<MessageReportedAudit>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<PMessage>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<WatchForum>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<WatchTopic>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Attachment>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<UserGroup>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<UserForum>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<NntpServer>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<NntpForum>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<NntpTopic>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<PMessage>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Replace_Words>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Spam_Words>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Registry>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<EventLog>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<BBCode>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Medal>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<GroupMedal>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<UserMedal>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<IgnoreUser>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<TopicReadTracking>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<UserPMessage>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<ForumReadTracking>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<ReputationVote>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<Tag>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<TopicTag>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<ProfileDefinition>());
            this.DbAccess.Execute(db => db.Connection.CreateTableIfNotExists<ProfileCustom>());
        }

        /// <summary>
        /// The execute script.
        /// </summary>
        /// <param name="scriptFile">
        /// The script file.
        /// </param>
        private void ExecuteScript([NotNull] string scriptFile)
        {
            string script;
            var fileName = this.Get<HttpRequestBase>().MapPath(scriptFile);

            try
            {
                script = $"{File.ReadAllText(fileName)}\r\n";
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (Exception x)
            {
                throw new IOException($"Failed to read {fileName}", x);
            }

            this.Get<IDbAccess>().SystemInitializeExecuteScripts(
                CommandTextHelpers.GetCommandTextReplaced(script),
                scriptFile,
                Config.SqlCommandTimeout);
        }

        /// <summary>
        ///    Add or Update BBCode Extensions and Spam Words
        /// </summary>
        private void AddOrUpdateExtensions()
        {
            var loadWrapper = new Action<string, Action<Stream>>(
                (file, streamAction) =>
                    {
                        var fullFile = this.Get<HttpRequestBase>().MapPath(file);

                        if (!File.Exists(fullFile))
                        {
                            return;
                        }

                        // import into board...
                        using var stream = new StreamReader(fullFile);
                        streamAction(stream.BaseStream);
                        stream.Close();
                    });

            // get all boards...
            var boardIds = this.GetRepository<Board>().GetAll().Select(x => x.ID);

            // Upgrade all Boards
            boardIds.ForEach(
                boardId =>
                    {
                        this.Get<IRaiseEvent>().Raise(new ImportStaticDataEvent(boardId));

                        // load default bbcode if available...
                        loadWrapper(BbcodeImport, s => DataImport.BBCodeExtensionImport(boardId, s));

                        // load default spam word if available...
                        loadWrapper(SpamWordsImport, s => DataImport.SpamWordsImport(boardId, s));
                    });
        }

        /// <summary>
        /// Migrate Legacy Membership Settings
        /// </summary>
        private void MigrateConfig()
        {
            try
            {
                var membershipSection = (MembershipSection)WebConfigurationManager.GetSection("system.web/membership");

                var defaultProvider = membershipSection.DefaultProvider;
                var hashAlgorithmType = membershipSection.HashAlgorithmType;

                var providerSettings = membershipSection.Providers[defaultProvider];

                var hashHex = providerSettings.Parameters["hashHex"];
                var hashCase = providerSettings.Parameters["hashCase"];

                ConfigHelper config = new ();

                if (hashAlgorithmType.IsSet())
                {
                    config.WriteAppSetting("YAF.LegacyMembershipHashAlgorithmType", hashAlgorithmType);
                }

                if (hashHex.IsSet())
                {
                    config.WriteAppSetting("YAF.LegacyMembershipHashHex", hashHex);
                }

                if (hashCase.IsSet())
                {
                    config.WriteAppSetting("YAF.LegacyMembershipHashCase", hashCase);
                }
            }
            catch (Exception)
            {
                // Can Be ignored if settings have already been removed
            }
        }

        #endregion
    }
}