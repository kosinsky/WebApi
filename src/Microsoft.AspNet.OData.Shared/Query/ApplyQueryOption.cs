// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.AspNet.OData.Common;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;

namespace Microsoft.AspNet.OData.Query
{
    /// <summary>
    /// This defines a $apply OData query option for querying.
    /// </summary>
    public class ApplyQueryOption
    {
        private ApplyClause _applyClause;
        private ODataQueryOptionParser _queryOptionParser;

        /// <summary>
        /// Initialize a new instance of <see cref="ApplyQueryOption"/> based on the raw $apply value and
        /// an EdmModel from <see cref="ODataQueryContext"/>.
        /// </summary>
        /// <param name="rawValue">The raw value for $filter query. It can be null or empty.</param>
        /// <param name="context">The <see cref="ODataQueryContext"/> which contains the <see cref="IEdmModel"/> and some type information</param>
        /// <param name="queryOptionParser">The <see cref="ODataQueryOptionParser"/> which is used to parse the query option.</param>
        public ApplyQueryOption(string rawValue, ODataQueryContext context, ODataQueryOptionParser queryOptionParser)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            if (String.IsNullOrEmpty(rawValue))
            {
                throw Error.ArgumentNullOrEmpty("rawValue");
            }

            if (queryOptionParser == null)
            {
                throw Error.ArgumentNull("queryOptionParser");
            }

            Context = context;
            RawValue = rawValue;
            // TODO: Implement and add validator
            //Validator = new FilterQueryValidator();
            _queryOptionParser = queryOptionParser;
            ResultClrType = Context.ElementClrType;
        }

        /// <summary>
        ///  Gets the given <see cref="ODataQueryContext"/>.
        /// </summary>
        public ODataQueryContext Context { get; private set; }

        /// <summary>
        /// ClrType for result of transformations
        /// </summary>
        public Type ResultClrType { get; private set; }

        /// <summary>
        /// Gets the parsed <see cref="ApplyClause"/> for this query option.
        /// </summary>
        public ApplyClause ApplyClause
        {
            get
            {
                if (_applyClause == null)
                {
                    _applyClause = _queryOptionParser.ParseApply();
                    if (_queryOptionParser.ParameterAliasNodes.Any())
                    {
                        List<TransformationNode> transformations = new List<TransformationNode>();
                        foreach (var transformation in _applyClause.Transformations)
                        {
                            if (transformation is FilterTransformationNode filterTransformation)
                            {
                                var filterClause = filterTransformation.FilterClause;
                                SingleValueNode filterExpression = filterClause.Expression.Accept(
                                    new ParameterAliasNodeTranslator(_queryOptionParser.ParameterAliasNodes)) as SingleValueNode;
                                filterExpression = filterExpression ?? new ConstantNode(null);
                                filterClause = new FilterClause(filterExpression, filterClause.RangeVariable);
                                transformations.Add(new FilterTransformationNode(filterClause));
                            }
                            else
                            {
                                transformations.Add(transformation);
                            }
                        }
                        _applyClause = new ApplyClause(transformations);
                    }
                }

                return _applyClause;
            }
        }

        internal SelectExpandClause SelectExpandClause { get; private set; }


        /// <summary>
        ///  Gets the raw $apply value.
        /// </summary>
        public string RawValue { get; private set; }

        /// <summary>
        /// Apply the apply query to the given IQueryable.
        /// </summary>
        /// <remarks>
        /// The <see cref="ODataQuerySettings.HandleNullPropagation"/> property specifies
        /// how this method should handle null propagation.
        /// </remarks>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <param name="querySettings">The <see cref="ODataQuerySettings"/> that contains all the query application related settings.</param>
        /// <returns>The new <see cref="IQueryable"/> after the filter query has been applied to.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling",
            Justification = "The majority of types referenced by this method are EdmLib types this method needs to know about to operate correctly")]
        public IQueryable ApplyTo(IQueryable query, ODataQuerySettings querySettings)
        {
            if (query == null)
            {
                throw Error.ArgumentNull("query");
            }

            if (querySettings == null)
            {
                throw Error.ArgumentNull("querySettings");
            }

            if (Context.ElementClrType == null)
            {
                throw Error.NotSupported(SRResources.ApplyToOnUntypedQueryOption, "ApplyTo");
            }

            // Linq to SQL not supported for $apply
            if (query.Provider.GetType().Namespace == HandleNullPropagationOptionHelper.Linq2SqlQueryProviderNamespace)
            {
                throw Error.NotSupported(SRResources.ApplyQueryOptionNotSupportedForLinq2SQL);
            }

            ApplyClause applyClause = ApplyClause;
            Contract.Assert(applyClause != null);

            ODataQuerySettings updatedSettings = Context.UpdateQuerySettings(querySettings, query);

            var binder = new ApplyQueryOptionsBinder(Context, updatedSettings, ResultClrType);
            query = binder.Bind(query, ApplyClause);
            this.ResultClrType = binder.ResultClrType;
            this.SelectExpandClause = binder.SelectExpandClause;

            return query;
        }
    }
}
