﻿using YAF.Lucene.Net.Documents;
using YAF.Lucene.Net.QueryParsers.Flexible.Core;
using YAF.Lucene.Net.QueryParsers.Flexible.Core.Config;
using YAF.Lucene.Net.QueryParsers.Flexible.Core.Messages;
using YAF.Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using YAF.Lucene.Net.QueryParsers.Flexible.Core.Processors;
using YAF.Lucene.Net.QueryParsers.Flexible.Core.Util;
using YAF.Lucene.Net.QueryParsers.Flexible.Standard.Config;
using YAF.Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using YAF.Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace YAF.Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// This processor is used to convert <see cref="TermRangeQueryNode"/>s to
    /// <see cref="NumericRangeQueryNode"/>s. It looks for
    /// <see cref="ConfigurationKeys.NUMERIC_CONFIG"/> set in the <see cref="FieldConfig"/> of
    /// every <see cref="TermRangeQueryNode"/> found. If
    /// <see cref="ConfigurationKeys.NUMERIC_CONFIG"/> is found, it considers that
    /// <see cref="TermRangeQueryNode"/> to be a numeric range query and convert it to
    /// <see cref="NumericRangeQueryNode"/>.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.NUMERIC_CONFIG"/>
    /// <seealso cref="TermRangeQueryNode"/>
    /// <seealso cref="NumericConfig"/>
    /// <seealso cref="NumericRangeQueryNode"/>
    public class NumericRangeQueryNodeProcessor : QueryNodeProcessor
    {
        /// <summary>
        /// Constructs an empty <see cref="NumericRangeQueryNode"/> object.
        /// </summary>
        public NumericRangeQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TermRangeQueryNode termRangeNode)
            {
                QueryConfigHandler config = GetQueryConfigHandler();

                if (config != null)
                {
                    FieldConfig fieldConfig = config.GetFieldConfig(StringUtils
                        .ToString(termRangeNode.Field));

                    if (fieldConfig != null)
                    {
                        NumericConfig numericConfig = fieldConfig
                            .Get(ConfigurationKeys.NUMERIC_CONFIG);

                        if (numericConfig != null)
                        {
                            FieldQueryNode lower = (FieldQueryNode)termRangeNode.LowerBound;
                            FieldQueryNode upper = (FieldQueryNode)termRangeNode.UpperBound;

                            string lowerText = lower.GetTextAsString();
                            string upperText = upper.GetTextAsString();
                            NumberFormat numberFormat = numericConfig.NumberFormat;
                            /*Number*/
                            object lowerNumber = null, upperNumber = null;

                            if (lowerText.Length > 0)
                            {
                                try
                                {
                                    lowerNumber = numberFormat.Parse(lowerText);
                                }
                                catch (FormatException e) // LUCENENET: In .NET we are expecting the framework to throw FormatException, not ParseException
                                {
                                    // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                                    throw new QueryNodeParseException(string.Format(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, lower
                                            .GetTextAsString(), numberFormat.ToString()), e);
                                }
                            }

                            if (upperText.Length > 0)
                            {
                                try
                                {
                                    upperNumber = numberFormat.Parse(upperText);
                                }
                                catch (FormatException e) // LUCENENET: In .NET we are expecting the framework to throw FormatException, not ParseException
                                {
                                    // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                                    throw new QueryNodeParseException(string.Format(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, upper
                                            .GetTextAsString(), numberFormat.ToString()), e);
                                }
                            }

                            switch (numericConfig.Type)
                            {
                                case NumericType.INT64:
                                    if (upperNumber != null) upperNumber = Convert.ToInt64(upperNumber); // LUCENENET TODO: Find a way to pass culture
                                    if (lowerNumber != null) lowerNumber = Convert.ToInt64(lowerNumber);
                                    break;
                                case NumericType.INT32:
                                    if (upperNumber != null) upperNumber = Convert.ToInt32(upperNumber); // LUCENENET TODO: Find a way to pass culture
                                    if (lowerNumber != null) lowerNumber = Convert.ToInt32(lowerNumber);
                                    break;
                                case NumericType.DOUBLE:
                                    if (upperNumber != null) upperNumber = Convert.ToDouble(upperNumber); // LUCENENET TODO: Find a way to pass culture
                                    if (lowerNumber != null) lowerNumber = Convert.ToDouble(lowerNumber);
                                    break;
                                case NumericType.SINGLE:
                                    if (upperNumber != null) upperNumber = Convert.ToSingle(upperNumber); // LUCENENET TODO: Find a way to pass culture
                                    if (lowerNumber != null) lowerNumber = Convert.ToSingle(lowerNumber);
                                    break;
                            }

                            NumericQueryNode lowerNode = new NumericQueryNode(
                                termRangeNode.Field, lowerNumber, numberFormat);
                            NumericQueryNode upperNode = new NumericQueryNode(
                                termRangeNode.Field, upperNumber, numberFormat);

                            bool lowerInclusive = termRangeNode.IsLowerInclusive;
                            bool upperInclusive = termRangeNode.IsUpperInclusive;

                            return new NumericRangeQueryNode(lowerNode, upperNode,
                                lowerInclusive, upperInclusive, numericConfig);
                        }
                    }
                }
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
