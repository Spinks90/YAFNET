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
namespace YAF.Types.Extensions
{
    #region Using

    using System;
    using System.Linq;
    using System.Security.Permissions;
    using System.Security.Policy;

    using YAF.Types;

    #endregion

    /// <summary>
    /// The type extensions.
    /// </summary>
    public static class TypeExtensions
    {
        #region Public Methods

        /// <summary>
        /// The get signing key.
        /// </summary>
        /// <param name="sourceType">
        /// The source type.
        /// </param>
        /// <returns>
        /// Returns the Signing Key
        /// </returns>
        public static StrongNamePublicKeyBlob GetSigningKey([NotNull] this Type sourceType)
        {
            CodeContracts.VerifyNotNull(sourceType);

            return sourceType.Assembly.Evidence.OfType<StrongName>().Select(t => t.PublicKey).FirstOrDefault();
        }

        #endregion
    }
}