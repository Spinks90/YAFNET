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
namespace YAF.Types.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     The pager extensions.
    /// </summary>
    public static class IPagerExtensions
    {
        #region Public Methods and Operators

        /// <summary>
        /// Uses the pager to convert the list into a properly skipped and paged list.
        /// </summary>
        /// <param name="list">
        /// The enumerable.
        /// </param>
        /// <param name="pager">
        /// The pager.
        /// </param>
        /// <typeparam name="T">
        /// The Typed Parameter
        /// </typeparam>
        /// <returns>
        /// The <see cref="IEnumerable{T}"/>.
        /// </returns>
        public static IList<T> GetPaged<T>([NotNull] this IList<T> list, [NotNull] IPager pager)
        {
            CodeContracts.VerifyNotNull(list);
            CodeContracts.VerifyNotNull(pager);

            pager.Count = list.Count;

            return list.Skip(pager.SkipIndex()).Take(pager.PageSize).ToList();
        }

        /// <summary>
        /// The page count.
        /// </summary>
        /// <param name="pager">
        /// The pager. 
        /// </param>
        /// <returns>
        /// The <see cref="int"/> . 
        /// </returns>
        public static int PageCount(this IPager pager)
        {
            CodeContracts.VerifyNotNull(pager);

            return PageCount(pager.Count, pager.PageSize);
        }

        /// <summary>
        /// Gets the Page Count from the Page Items Count and Page Size
        /// </summary>
        /// <param name="pageItemsCount">
        /// The page Items Count.
        /// </param>
        /// <param name="pageSize">
        /// Size of the page.
        /// </param>
        /// <returns>
        /// Returns the Pages Count
        /// </returns>
        public static int PageCount(int pageItemsCount, int pageSize)
        {
            return (int)Math.Ceiling((double)pageItemsCount / pageSize);
        }

        /// <summary>
        /// The skip index.
        /// </summary>
        /// <param name="pager">
        /// The pager. 
        /// </param>
        /// <returns>
        /// The <see cref="int"/> . 
        /// </returns>
        public static int SkipIndex([NotNull] this IPager pager)
        {
            CodeContracts.VerifyNotNull(pager);

            return (int)Math.Ceiling((double)pager.CurrentPageIndex * pager.PageSize);
        }

        #endregion
    }
}