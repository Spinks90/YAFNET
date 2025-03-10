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
namespace YAF.Core.Model
{
    using System;
    using System.Collections.Generic;

    using ServiceStack.OrmLite;

    using YAF.Core.Context;
    using YAF.Core.Extensions;
    using YAF.Types;
    using YAF.Types.Constants;
    using YAF.Types.Extensions;
    using YAF.Types.Interfaces;
    using YAF.Types.Interfaces.Data;
    using YAF.Types.Models;

    /// <summary>
    ///     The Registry repository extensions.
    /// </summary>
    public static class RegistryRepositoryExtensions
    {
        #region Public Methods and Operators

        /// <summary>
        /// Update Max User Stats
        /// </summary>
        /// <param name="repository">
        /// The repository.
        /// </param>
        /// <param name="boardId">
        /// The board id.
        /// </param>
        public static void UpdateMaxStats(this IRepository<Registry> repository, [NotNull] int boardId)
        {
            CodeContracts.VerifyNotNull(repository);

            var count = repository.DbAccess.Execute(
                db =>
                {
                    var expression = OrmLiteConfig.DialectProvider.SqlExpression<Active>();

                    expression.Where(x => x.BoardID == boardId);

                    return db.Connection.Scalar<int>(expression.Select(x => Sql.CountDistinct(x.IP)));
                });

            var maxUsers = BoardContext.Current.BoardSettings.MaxUsers;

            if (count <= maxUsers)
            {
                return;
            }

            repository.Save("maxusers", count, boardId);
            repository.Save("maxuserswhen", DateTime.UtcNow.ToString("u"), boardId);
        }

        /// <summary>
        /// Increment the the Denied User Registration Count.
        /// </summary>
        /// <param name="repository">
        /// The repository.
        /// </param>
        public static void IncrementDeniedRegistrations(this IRepository<Registry> repository)
        {
            CodeContracts.VerifyNotNull(repository);

            BoardContext.Current.BoardSettings.DeniedRegistrations++;

            repository.Save(
                "DeniedRegistrations",
                BoardContext.Current.BoardSettings.DeniedRegistrations,
                repository.BoardID);
        }

        /// <summary>
        /// Increment the Banned users count.
        /// </summary>
        /// <param name="repository">
        /// The repository.
        /// </param>
        public static void IncrementBannedUsers(this IRepository<Registry> repository)
        {
            CodeContracts.VerifyNotNull(repository);

            BoardContext.Current.BoardSettings.BannedUsers++;

            repository.Save(
                "BannedUsers",
                BoardContext.Current.BoardSettings.BannedUsers,
                repository.BoardID);
        }

        /// <summary>
        /// Increment the reported spammers count.
        /// </summary>
        /// <param name="repository">
        /// The repository.
        /// </param>
        public static void IncrementReportedSpammers(this IRepository<Registry> repository)
        {
            CodeContracts.VerifyNotNull(repository);

            BoardContext.Current.BoardSettings.ReportedSpammers++;

            repository.Save(
                "ReportedSpammers",
                BoardContext.Current.BoardSettings.ReportedSpammers,
                repository.BoardID);
        }

        /// <summary>
        /// The list.
        /// </summary>
        /// <param name="repository">
        /// The repository.
        /// </param>
        /// <param name="boardId">
        /// The board id.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        public static List<Registry> List(this IRepository<Registry> repository, [CanBeNull] int? boardId = null)
        {
            CodeContracts.VerifyNotNull(repository);

            return repository.Get(r => r.BoardID == boardId);
        }

        /// <summary>
        /// Saves the specified repository.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="settingName">Name of the setting.</param>
        /// <param name="settingValue">The setting value.</param>
        /// <param name="boardId">The board identifier.</param>
        public static void Save(
            this IRepository<Registry> repository,
            [NotNull] string settingName,
            [CanBeNull] object settingValue,
            [CanBeNull] int? boardId = null)
        {
            CodeContracts.VerifyNotNull(repository);

            settingValue ??= string.Empty;

            if (boardId.HasValue)
            {
                var registry = repository.GetSingle(
                    r => r.Name.ToLower() == settingName.ToLower() && r.BoardID == boardId.Value);

                if (registry != null)
                {
                    registry.Value = settingValue.ToString();

                    repository.Update(registry);
                }
                else
                {
                    repository.Insert(
                        new Registry
                        {
                            BoardID = boardId.Value, Name = settingName.ToLower(), Value = settingValue.ToString()
                        });
                }
            }
            else
            {
                var registry = repository.GetSingle(
                    r => r.Name.ToLower() == settingName.ToLower() && r.BoardID == null);

                if (registry != null)
                {
                    registry.Value = settingValue.ToString();

                    repository.Update(registry);
                }
                else
                {
                    repository.Insert(
                        new Registry
                        {
                            Name = settingName.ToLower(),
                            Value = settingValue.ToString()
                        });
                }
            }

            repository.FireUpdated();
        }

        /// <summary>
        /// Gets the Current DB Version Name
        /// </summary>
        /// <param name="repository">
        /// The repository.
        /// </param>
        /// <returns>
        /// Returns the Current DB Version Name
        /// </returns>
        public static string GetDbVersionName(this IRepository<Registry> repository)
        {
            CodeContracts.VerifyNotNull(repository);

            return repository.GetSingle(r => r.Name.ToLower() == "versionname").Value;
        }

        /// <summary>
        /// Gets the Current YAF DB Version
        /// </summary>
        /// <param name="repository">
        /// The repository.
        /// </param>
        /// <returns>
        /// Returns the Current YAF DB Version
        /// </returns>
        public static int GetDbVersion(this IRepository<Registry> repository)
        {
            CodeContracts.VerifyNotNull(repository);

            int version;

            try
            {
                var registry = repository.GetSingle(r => r.Name.ToLower() == "version");

                if (registry == null)
                {
                    version = -1;
                }
                else
                {
                    version = registry.Value.ToType<int>();
                }
            }
            catch (Exception)
            {
                version = -1;
            }

            return version;
        }

        /// <summary>
        /// The validate version.
        /// </summary>
        /// <param name="repository">
        ///     The repository.
        /// </param>
        /// <param name="appVersion">
        ///     The app version.
        /// </param>
        /// <param name="registryVersion">Returns Current Registry Version</param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string ValidateVersion(
            this IRepository<Registry> repository,
            [NotNull] int appVersion,
            out int registryVersion)
        {
            CodeContracts.VerifyNotNull(repository);

            var redirect = string.Empty;

            try
            {
                registryVersion = repository.GetCurrentVersion();

                if (registryVersion < appVersion)
                {
                    // needs upgrading...
                    redirect = $"install/default.aspx?upgrade={registryVersion}";
                }
            }
            catch (Exception)
            {
                registryVersion = 0;

                // needs to be setup...
                redirect = "install/default.aspx";
            }


            return redirect;
        }

        /// <summary>
        /// Gets the current YAF DB version.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <returns>Returns the YAF DB Version</returns>
        public static int GetCurrentVersion(this IRepository<Registry> repository)
        {
            return BoardContext.Current.Get<IDataCache>().GetOrSet(
                Constants.Cache.Version,
                () => repository.GetSingle(r => r.Name.ToLower() == "version").Value.ToType<int>());
        }

        /// <summary>
        /// Delete old registry Settings
        /// </summary>
        /// <param name="repository">
        /// The repository.
        /// </param>
        public static void DeleteLegacy(this IRepository<Registry> repository)
        {
            CodeContracts.VerifyNotNull(repository);

            repository.Delete(x => x.Name == "smtpserver");
            repository.Delete(x => x.Name == "avatarremote");
            repository.Delete(x => x.Name == "enablethanksmod");
            repository.Delete(x => x.Name == "topicsperpage");
            repository.Delete(x => x.Name == "MemberListPageSize".ToLower());
            repository.Delete(x => x.Name == "MyTopicsListPageSize".ToLower());
            repository.Delete(x => x.Name == "EnableTopicDescription".ToLower());
            repository.Delete(x => x.Name == "MaxWordLength".ToLower());
        }

        #endregion
    }
}