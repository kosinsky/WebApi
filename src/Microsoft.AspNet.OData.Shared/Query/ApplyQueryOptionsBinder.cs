// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.OData.Adapters;
using Microsoft.AspNet.OData.Interfaces;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;

namespace Microsoft.AspNet.OData.Query
{
    internal class ApplyQueryOptionsBinder
    {
        ODataQueryContext _context;
        ODataQuerySettings _settings;
        IWebApiAssembliesResolver _assembliesResolver;
        

        public ApplyQueryOptionsBinder(ODataQueryContext context, ODataQuerySettings settings, Type clrType)
        {
            this._context = context;
            this._settings = settings;
            this.ResultClrType = clrType;
            //this._assembliesResolver = assembliesResolver;
            // The IWebApiAssembliesResolver service is internal and can only be injected by WebApi.
            // This code path may be used in cases when the service container is not available
            // and the service container is available but may not contain an instance of IWebApiAssembliesResolver.
            _assembliesResolver = WebApiAssembliesResolver.Default;
            if (_context.RequestContainer != null)
            {
                IWebApiAssembliesResolver injectedResolver = _context.RequestContainer.GetService<IWebApiAssembliesResolver>();
                if (injectedResolver != null)
                {
                    _assembliesResolver = injectedResolver;
                }
            }
        }

        public Type ResultClrType { get; private set; }

        internal SelectExpandClause SelectExpandClause { get; private set; }

        public IQueryable Bind(IQueryable query, ApplyClause applyClause)
        {

            // groupby and aggregate transform input  by collapsing everything not used in groupby/aggregate 
            // as a result we have to distinct cases for expand implementation
            // 1. Expands followed by groupby/aggregate with entity set aggregations => filters in expand need to be applied (pushed down) to corresponding entityset aggregations 
            // 2. Mix of expands and filters w/o any groupby/aggregation => falling back to $expand behavior and could just use SelectExpandBinder
            bool inputShapeChanged = false;

            foreach (var transformation in applyClause.Transformations)
            {
                if (transformation.Kind == TransformationNodeKind.Aggregate || transformation.Kind == TransformationNodeKind.GroupBy)
                {
                    var binder = new AggregationBinder(_settings, _assembliesResolver, ResultClrType, _context.Model, transformation, _context, SelectExpandClause);
                    query = binder.Bind(query);
                    this.ResultClrType = binder.ResultClrType;
                    inputShapeChanged = true;
                }
                else if (transformation.Kind == TransformationNodeKind.Compute)
                {
                    var binder = new ComputeBinder(_settings, _assembliesResolver, ResultClrType, _context.Model, (ComputeTransformationNode)transformation);
                    query = binder.Bind(query);
                    this.ResultClrType = binder.ResultClrType;
                    inputShapeChanged = true;
                }
                else if (transformation.Kind == TransformationNodeKind.Filter)
                {
                    var filterTransformation = transformation as FilterTransformationNode;
                    Expression filter = FilterBinder.Bind(query, filterTransformation.FilterClause, ResultClrType, _context, _settings);
                    query = ExpressionHelpers.Where(query, filter, ResultClrType);
                }
                else if (transformation.Kind == TransformationNodeKind.Expand)
                {
                    var newClause = ((ExpandTransformationNode)transformation).ExpandClause;
                    if (SelectExpandClause == null)
                    {
                        SelectExpandClause = newClause;
                    }
                    else
                    {
                        SelectExpandClause = new SelectExpandClause(SelectExpandClause.SelectedItems.Concat(newClause.SelectedItems), false);
                    }
                }
            }

            if (SelectExpandClause != null && !inputShapeChanged)
            {
                var expandString = GetExpandsOnlyString(SelectExpandClause);

                var selectExpandQueryOption = new SelectExpandQueryOption(null, expandString, _context, SelectExpandClause);
                query = SelectExpandBinder.Bind(query, _settings, selectExpandQueryOption);
            }

            return query;

        }


        private static string GetExpandsOnlyString(SelectExpandClause selectExpandClause)
        {
            string result = "$expand=";

            foreach (var item in selectExpandClause.SelectedItems.OfType<ExpandedNavigationSelectItem>())
            {
                result += item.NavigationSource.Name;
            }

            return result;
        }
    }
}
