// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNet.OData.Common;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Interfaces;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;

namespace Microsoft.AspNet.OData.Query.Expressions
{
    internal class ComputeBinder : TransformationBinderBase
    {
        private const string GroupByContainerProperty = "GroupByContainer";

        private ComputeTransformationNode _transformation;

        internal ComputeBinder(ODataQuerySettings settings, IWebApiAssembliesResolver assembliesResolver, Type elementType,
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
            // compute(X add Y as Z, A mul B as C) adds new properties to the output
            // Should return following expression
            // .Select($it => new ComputeWrapper<T> {
            //      Instance = $it,
            //      Container => new AggregationPropertyContainer() {
            //          Name = "X", 
            //          Value = $it.X + $it.Y, 
            //          Next = new LastInChain() {
            //              Name = "C",
            //              Value = $it.A * $it.B
            //      }
            // })

            List<MemberAssignment> wrapperTypeMemberAssignments = new List<MemberAssignment>();

            // Set Instance property
            var wrapperProperty = this.ResultClrType.GetProperty("Instance");
            wrapperTypeMemberAssignments.Add(Expression.Bind(wrapperProperty, this._lambdaParameter));
            var properties = new List<NamedPropertyExpression>();
            foreach (var computeExpression in this._transformation.ComputeClause.ComputedItems)
            {
                properties.Add(new NamedPropertyExpression(Expression.Constant(computeExpression.Alias), CreateComputeExpression(computeExpression)));
            }

            // Set new compute properties
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
