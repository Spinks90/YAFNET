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

namespace YAF.Data.MySql
{
    using ServiceStack.OrmLite;

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;

    using global::MySql.Data.MySqlClient;

    using YAF.Core.Data;
    using YAF.Types;
    using YAF.Types.Interfaces.Data;
    using YAF.Types.Models;

    using Config = YAF.Configuration.Config;

    /// <summary>
    /// MySQL DB Information
    /// </summary>
    public class MySqlDbInformation : IDbInformation
    {
        /// <summary>
        /// The YAF Provider Upgrade script list
        /// </summary>
        private static readonly string[] IdentityUpgradeScriptList = {
            "mssql/upgrade/identity/upgrade.sql"
        };

        /// <summary>
        /// The DB parameters
        /// </summary>
        private readonly DbConnectionParam[] connectionParameters = {
            new(0, "Password", string.Empty),
            new(1, "Data Source", "(local)")
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlDbInformation"/> class.
        /// </summary>
        public MySqlDbInformation()
        {
            this.ConnectionString = () => Config.ConnectionString;
            this.ProviderName = MySqlDbAccess.ProviderTypeName;
        }

        /// <summary>
        /// Gets or sets the DB Connection String
        /// </summary>
        public Func<string> ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the DB Provider Name
        /// </summary>
        public string ProviderName { get; protected set; }

        /// <summary>
        /// Gets the YAF Provider Upgrade Script List.
        /// </summary>
        public IEnumerable<string> IdentityUpgradeScripts => IdentityUpgradeScriptList;

        /// <summary>
        /// Gets the DB Connection Parameters.
        /// </summary>
        public IDbConnectionParam[] DbConnectionParameters =>
            this.connectionParameters.OfType<IDbConnectionParam>().ToArray();

        /// <summary>
        /// Builds a connection string.
        /// </summary>
        /// <param name="parameters">The Connection Parameters</param>
        /// <returns>Returns the Connection String</returns>
        public string BuildConnectionString([NotNull] IEnumerable<IDbConnectionParam> parameters)
        {
            var connectionParams = parameters.ToList();

            CodeContracts.VerifyNotNull(connectionParams);

            var connBuilder = new MySqlConnectionStringBuilder();

            connectionParams.ForEach(param => connBuilder[param.Name] = param.Value);

            return connBuilder.ConnectionString;
        }

        /// <summary>
        /// Create Table Views
        /// </summary>
        /// <param name="dbAccess">
        /// The database access.
        /// </param>
        /// <param name="dbCommand">
        /// The database command.
        /// </param>
        public bool CreateViews(IDbAccess dbAccess, IDbCommand dbCommand)
        {
            var vaccessGroupSelect = new StringBuilder();

            vaccessGroupSelect.Append(" select ");

            vaccessGroupSelect.Append("e.BoardID,");
            vaccessGroupSelect.Append("b.UserID,");
            vaccessGroupSelect.Append("c.ForumID,");
            vaccessGroupSelect.Append("d.AccessMaskID,");
            vaccessGroupSelect.Append("b.GroupID,");
            vaccessGroupSelect.Append("d.Flags & 1 AS ReadAccess,");
            vaccessGroupSelect.Append("d.Flags & 2 AS PostAccess,");
            vaccessGroupSelect.Append("d.Flags & 4 AS ReplyAccess,");
            vaccessGroupSelect.Append("d.Flags & 8 AS PriorityAccess,");
            vaccessGroupSelect.Append("d.Flags & 16 AS PollAccess,");
            vaccessGroupSelect.Append("d.Flags & 32 AS VoteAccess,");
            vaccessGroupSelect.Append("d.Flags & 64 AS ModeratorAccess,");
            vaccessGroupSelect.Append("d.Flags & 128 AS EditAccess,");
            vaccessGroupSelect.Append("d.Flags & 256 AS DeleteAccess,");
            vaccessGroupSelect.Append("d.Flags & 512 AS UploadAccess,");
            vaccessGroupSelect.Append("d.Flags & 1024 AS DownloadAccess,");
            vaccessGroupSelect.Append("e.Flags & 1 AS AdminGroup");

            vaccessGroupSelect.Append(" from");

            vaccessGroupSelect.AppendFormat(
                " {0}.{1}UserGroup AS b",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);
            vaccessGroupSelect.AppendFormat(
                " INNER JOIN {0}.{1}ForumAccess AS c on c.GroupID=b.GroupID",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);
            vaccessGroupSelect.AppendFormat(
                " INNER JOIN {0}.{1}AccessMask AS d on d.AccessMaskID=c.AccessMaskID",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);
            vaccessGroupSelect.AppendFormat(
                " INNER JOIN {0}.{1}Group AS e on e.GroupID=b.GroupID",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            dbCommand.Connection.CreateView<vaccess_group>(vaccessGroupSelect);

            var vaccessNullSelect = new StringBuilder();

            vaccessNullSelect.Append(" select ");

            vaccessNullSelect.Append("a.UserID,");
            vaccessNullSelect.Append("0 AS ForumID,");
            vaccessNullSelect.Append("0 AS ReadAccess,");
            vaccessNullSelect.Append("0 AS PostAccess,");
            vaccessNullSelect.Append("0 AS ReplyAccess,");
            vaccessNullSelect.Append("0 AS PriorityAccess,");
            vaccessNullSelect.Append("0 AS PollAccess,");
            vaccessNullSelect.Append("0 AS VoteAccess,");
            vaccessNullSelect.Append("0 AS ModeratorAccess,");
            vaccessNullSelect.Append("0 AS EditAccess,");
            vaccessNullSelect.Append("0 AS DeleteAccess,");
            vaccessNullSelect.Append("0 AS UploadAccess,");
            vaccessNullSelect.Append("0 AS DownloadAccess,");
            vaccessNullSelect.Append("0 AS AdminGroup ");

            vaccessNullSelect.Append(" from");

            vaccessNullSelect.AppendFormat(" {0}.{1}User AS a", dbCommand.Connection.Database, Config.DatabaseObjectQualifier);

            dbCommand.Connection.CreateView<vaccess_null>(vaccessNullSelect);

            var vaccessUserSelect = new StringBuilder();

            vaccessUserSelect.Append(" select ");

            vaccessUserSelect.Append("b.UserID,");
            vaccessUserSelect.Append("b.ForumID,");
            vaccessUserSelect.Append("c.AccessMaskID,");
            vaccessNullSelect.Append("0 AS GroupID,");
            vaccessUserSelect.Append("c.Flags & 1 AS ReadAccess,");
            vaccessUserSelect.Append("c.Flags & 2 AS PostAccess,");
            vaccessUserSelect.Append("c.Flags & 4 AS ReplyAccess,");
            vaccessUserSelect.Append("c.Flags & 8 AS PriorityAccess,");
            vaccessUserSelect.Append("c.Flags & 16 AS PollAccess,");
            vaccessUserSelect.Append("c.Flags & 32 AS VoteAccess,");
            vaccessUserSelect.Append("c.Flags & 64 AS ModeratorAccess,");
            vaccessUserSelect.Append("c.Flags & 128 AS EditAccess,");
            vaccessUserSelect.Append("c.Flags & 256 AS DeleteAccess,");
            vaccessUserSelect.Append("c.Flags & 512 AS UploadAccess,");
            vaccessUserSelect.Append("c.Flags & 1024 AS DownloadAccess");

            vaccessUserSelect.Append(" from");
            vaccessUserSelect.AppendFormat(
                " {0}.{1}UserForum AS b",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);
            vaccessUserSelect.AppendFormat(
                " INNER JOIN {0}.{1}AccessMask AS c on c.AccessMaskID=b.AccessMaskID",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            dbCommand.Connection.CreateView<vaccess_user>(vaccessUserSelect);

            var vaccessFullSelect = new StringBuilder();



            vaccessFullSelect.Append(" select ");

            vaccessFullSelect.Append("b.UserID,");
            vaccessFullSelect.Append("b.ForumID,");
            vaccessFullSelect.Append("c.Flags & 1 AS ReadAccess,");
            vaccessFullSelect.Append(" c.Flags & 2 AS PostAccess,");
            vaccessFullSelect.Append("c.Flags & 4 AS ReplyAccess,");
            vaccessFullSelect.Append("c.Flags & 8 AS PriorityAccess,");
            vaccessFullSelect.Append("c.Flags & 16 AS PollAccess,");
            vaccessFullSelect.Append("c.Flags & 32 AS VoteAccess,");
            vaccessFullSelect.Append("c.Flags & 64 AS ModeratorAccess,");
            vaccessFullSelect.Append("c.Flags & 128 AS EditAccess,");
            vaccessFullSelect.Append("c.Flags & 256 AS DeleteAccess,");
            vaccessFullSelect.Append("c.Flags & 512 AS UploadAccess,");
            vaccessFullSelect.Append("c.Flags & 1024 AS DownloadAccess,");
            vaccessFullSelect.Append("0 AS AdminGroup ");

            vaccessFullSelect.AppendFormat("FROM {0}.{1}UserForum AS b ",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessFullSelect.AppendFormat("INNER JOIN {0}.{1}AccessMask AS c ",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessFullSelect.Append("ON c.AccessMaskID = b.AccessMaskID ");

            vaccessFullSelect.Append("UNION ALL ");
            vaccessFullSelect.Append(" SELECT ");

            vaccessFullSelect.Append("b.UserID,");
            vaccessFullSelect.Append("c.ForumID,");
            vaccessFullSelect.Append("d.Flags & 1 AS ReadAccess,");
            vaccessFullSelect.Append("d.Flags & 2 AS PostAccess,");
            vaccessFullSelect.Append("d.Flags & 4 AS ReplyAccess,");
            vaccessFullSelect.Append("d.Flags & 8 AS PriorityAccess,");
            vaccessFullSelect.Append("d.Flags & 16 AS PollAccess,");
            vaccessFullSelect.Append("d.Flags & 32 AS VoteAccess,");
            vaccessFullSelect.Append("d.Flags & 64 AS ModeratorAccess,");
            vaccessFullSelect.Append("d.Flags & 128 AS EditAccess,");
            vaccessFullSelect.Append("d.Flags & 256 AS DeleteAccess,");
            vaccessFullSelect.Append("d.Flags & 512 AS UploadAccess,");
            vaccessFullSelect.Append("d.Flags & 1024 AS DownloadAccess,");
            vaccessFullSelect.Append("e.Flags & 1 AS AdminGroup");

            vaccessFullSelect.AppendFormat(" FROM {0}.{1}UserGroup AS b",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessFullSelect.AppendFormat(" INNER JOIN {0}.{1}ForumAccess AS c ON c.GroupID = b.GroupID ",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessFullSelect.AppendFormat(" INNER JOIN {0}.{1}AccessMask AS d ON d.AccessMaskID = c.AccessMaskID ",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessFullSelect.AppendFormat(" INNER JOIN {0}.{1}Group e ON e.GroupID = b.GroupID ",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessFullSelect.Append(" UNION ALL ");
            vaccessFullSelect.Append(" SELECT ");

            vaccessFullSelect.Append("UserID,");
            vaccessFullSelect.Append("0 AS ForumID,");
            vaccessFullSelect.Append("0 AS ReadAccess,");
            vaccessFullSelect.Append("0 AS PostAccess,");
            vaccessFullSelect.Append("0 AS ReplyAccess,");
            vaccessFullSelect.Append("0 AS PriorityAccess,");
            vaccessFullSelect.Append("0 AS PollAccess,");
            vaccessFullSelect.Append("0 AS VoteAccess,");
            vaccessFullSelect.Append("0 AS ModeratorAccess,");
            vaccessFullSelect.Append("0 AS EditAccess,");
            vaccessFullSelect.Append("0 AS DeleteAccess,");
            vaccessFullSelect.Append(" 0 AS UploadAccess,");
            vaccessFullSelect.Append("0 AS DownloadAccess,");
            vaccessFullSelect.Append("0 AS AdminGroup ");

            vaccessFullSelect.AppendFormat("FROM {0}.{1}User AS a",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            dbCommand.Connection.CreateView<vaccessfull>(vaccessFullSelect);

            var vaccessSelect = new StringBuilder();

            vaccessSelect.Append(" select ");

            vaccessSelect.Append("a.UserID,");
            vaccessSelect.Append("x_1.ForumID,");
            vaccessSelect.Append("MAX(b.Flags & 1) AS IsAdmin,");
            vaccessSelect.Append("MAX(b.Flags & 2) AS IsGuest,");
            vaccessSelect.Append("MAX(b.Flags & 8) AS IsForumModerator,");
            vaccessSelect.Append("(SELECT COUNT(1) AS Expr1 ");

            vaccessSelect.AppendFormat("FROM {0}.{1}UserGroup AS v",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);
            vaccessSelect.AppendFormat(" INNER JOIN {0}.{1}Group AS w ON v.GroupID = w.GroupID",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessSelect.AppendFormat(" CROSS JOIN  {0}.{1}ForumAccess AS x",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessSelect.AppendFormat(" CROSS JOIN  {0}.{1}AccessMask AS y",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessSelect.Append(" WHERE(v.UserID = a.UserID)");
            vaccessSelect.Append(" AND(x.GroupID = w.GroupID)");
            vaccessSelect.Append(" AND(y.AccessMaskID = x.AccessMaskID)");

            vaccessSelect.Append(" AND(y.Flags & 64 <> 0)) AS IsModerator,");
            vaccessSelect.Append("MAX(x_1.ReadAccess) AS ReadAccess,");
            vaccessSelect.Append("MAX(x_1.PostAccess) AS PostAccess,");
            vaccessSelect.Append("MAX(x_1.ReplyAccess) AS ReplyAccess,");
            vaccessSelect.Append("MAX(x_1.PriorityAccess) AS PriorityAccess,");
            vaccessSelect.Append("MAX(x_1.PollAccess) AS PollAccess,");
            vaccessSelect.Append("MAX(x_1.VoteAccess) AS VoteAccess,");
            vaccessSelect.Append("MAX(x_1.ModeratorAccess) AS ModeratorAccess,");
            vaccessSelect.Append("MAX(x_1.EditAccess) AS EditAccess,");
            vaccessSelect.Append("MAX(x_1.DeleteAccess) AS DeleteAccess,");
            vaccessSelect.Append("MAX(x_1.UploadAccess) AS UploadAccess,");
            vaccessSelect.Append("MAX(x_1.DownloadAccess) AS DownloadAccess ");

            vaccessSelect.AppendFormat(" FROM {0}.{1}vaccessfull x_1 ",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessSelect.AppendFormat(" INNER JOIN  {0}.{1}UserGroup AS a ON a.UserID = x_1.UserID ",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessSelect.AppendFormat(" INNER JOIN {0}.{1}Group AS b ON b.GroupID = a.GroupID ",
                dbCommand.Connection.Database,
                Config.DatabaseObjectQualifier);

            vaccessSelect.Append(" GROUP BY a.UserID, x_1.ForumID");

            dbCommand.Connection.CreateView<vaccess>(vaccessSelect);

            return true;
        }

        /// <summary>
        /// Create Indexes on Table Views
        /// </summary>
        /// <param name="dbAccess">
        /// The database access.
        /// </param>
        /// <param name="dbCommand">
        /// The database command.
        /// </param>
        public bool CreateIndexViews(IDbAccess dbAccess, IDbCommand dbCommand)
        {
            return true;
        }
    }
}