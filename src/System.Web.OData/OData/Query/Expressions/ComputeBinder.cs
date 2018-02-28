// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.OData.Formatter;
using System.Web.OData.Properties;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;

namespace System.Web.OData.Query.Expressions
{
    internal class ComputeBinder : TransformationBinderBase
    {
        private const string GroupByContainerProperty = "GroupByContainer";

        private ComputeTransformationNode _transformation;

        internal ComputeBinder(ODataQuerySettings settings, IAssembliesResolver assembliesResolver, Type elementType,
            IEdmModel model, ComputeTransformationNode transformation)
            : base(settings, assembliesResolver, elementType, model)
        {
            Contract.Assert(transformation != null);
            
            _transformation = transformation;

            this.ResultClrType = typeof(ComputeWrapper<>).MakeGenericType(this._elementType);
        }

        public IQueryable Bind(IQueryable query)
        {
            PreprocessQuery(query);

            List<MemberAssignment> wrapperTypeMemberAssignments = new List<MemberAssignment>();

            var wrapperProperty = this.ResultClrType.GetProperty("Instance");

            wrapperTypeMemberAssignments.Add(Expression.Bind(wrapperProperty, this._lambdaParameter));

            var properties = new List<NamedPropertyExpression>();
            foreach (var computeExpression in this._transformation.ComputeClause.ComputedItems)
            {
                properties.Add(new NamedPropertyExpression(Expression.Constant(computeExpression.Alias), CreateComputeExpression(computeExpression)));
            }

            wrapperProperty = ResultClrType.GetProperty("Container");
            wrapperTypeMemberAssignments.Add(Expression.Bind(wrapperProperty, AggregationPropertyContainer.CreateNextNamedPropertyContainer(properties)));

            var initilizedMember =
                Expression.MemberInit(Expression.New(ResultClrType), wrapperTypeMemberAssignments);
            var selectLambda = Expression.Lambda(initilizedMember, this._lambdaParameter);

            var result = ExpressionHelpers.Select(query, selectLambda, this._elementType);
            return result;
        }

        private Expression CreateComputeExpression(ComputeExpression expression)
        {
            Expression body = BindAccessor(expression.Expression);
            return WrapConvert(body);
        }
    }
}
