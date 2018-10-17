// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNet.OData.Common;
using Microsoft.AspNet.OData.Interfaces;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.AspNet.OData.Query.Expressions
{
    internal class TransformationBinderBase : ExpressionBinderBase
    {
        internal TransformationBinderBase(ODataQuerySettings settings, IWebApiAssembliesResolver assembliesResolver, Type elementType,
            IEdmModel model) : base(model, assembliesResolver, settings)
        {
            Contract.Assert(elementType != null);
            _elementType = elementType;

            this._lambdaParameter = Expression.Parameter(this._elementType, "$it");
        }

        protected Type _elementType;
        private bool _linqToObjectMode = false;
        protected ParameterExpression _lambdaParameter;

        /// <summary>
        /// Gets CLR type returned from the query.
        /// </summary>
        public Type ResultClrType
        {
            get; protected set;
        }

        protected void PreprocessQuery(IQueryable query)
        {
            Contract.Assert(query != null);

            this._linqToObjectMode = query.Provider.GetType().Namespace == HandleNullPropagationOptionHelper.Linq2ObjectsQueryProviderNamespace;
            this.BaseQuery = query;
            EnsureFlattenedPropertyContainer(this._lambdaParameter);
        }

        protected Expression WrapConvert(Expression expression)
        {
            return this._linqToObjectMode
                ? Expression.Convert(expression, typeof(object))
                : expression;
        }

        public override Expression Bind(QueryNode node)
        {
            SingleValueNode singleValueNode = node as SingleValueNode;
            if (node != null)
            {
                return BindAccessor(singleValueNode);
            }

            throw new ArgumentException("Only SigleValueNode supported", "node");
        }

        protected override ParameterExpression ItParameter
        {
            get
            {
                return this._lambdaParameter;
            }
        }

        protected Expression BindAccessor(SingleValueNode node)
        {
            switch (node.Kind)
            {
                case QueryNodeKind.ResourceRangeVariableReference:
                    return this._lambdaParameter.Type.IsGenericType && this._lambdaParameter.Type.GetGenericTypeDefinition() == typeof(FlatteningWrapper<>)
                        ? (Expression)Expression.Property(this._lambdaParameter, "Source")
                        : this._lambdaParameter;
                case QueryNodeKind.SingleValuePropertyAccess:
                    var propAccessNode = node as SingleValuePropertyAccessNode;
                    return CreatePropertyAccessExpression(BindAccessor(propAccessNode.Source), propAccessNode.Property, GetFullPropertyPath(propAccessNode));
                case QueryNodeKind.SingleComplexNode:
                    var singleComplexNode = node as SingleComplexNode;
                    return CreatePropertyAccessExpression(BindAccessor(singleComplexNode.Source), singleComplexNode.Property, GetFullPropertyPath(singleComplexNode));
                case QueryNodeKind.SingleValueOpenPropertyAccess:
                    var openNode = node as SingleValueOpenPropertyAccessNode;
                    return GetFlattenedPropertyExpression(openNode.Name) ?? CreateOpenPropertyAccessExpression(openNode);
                case QueryNodeKind.None:
                case QueryNodeKind.SingleNavigationNode:
                    var navNode = (SingleNavigationNode)node;
                    return CreatePropertyAccessExpression(BindAccessor(navNode.Source), navNode.NavigationProperty);
                case QueryNodeKind.BinaryOperator:
                    var binaryNode = (BinaryOperatorNode)node;
                    return BindBinaryOperatorNode(binaryNode);
                case QueryNodeKind.Convert:
                    var convertNode = (ConvertNode)node;
                    return CreateConvertExpression(convertNode, BindAccessor(convertNode.Source));
                case QueryNodeKind.SingleValueFunctionCall:
                    return BindSingleValueFunctionCallNode(node as SingleValueFunctionCallNode);
                case QueryNodeKind.Constant:
                    return BindConstantNode(node as ConstantNode);
                default:
                    throw Error.NotSupported(SRResources.QueryNodeBindingNotSupported, node.Kind,
                        typeof(AggregationBinder).Name);
            }
        }

        private Expression CreateOpenPropertyAccessExpression(SingleValueOpenPropertyAccessNode openNode)
        {
            Expression sourceAccessor = BindAccessor(openNode.Source);

            // First check that property exists in source
            // It's the case when we are apply transformation based on earlier transformation
            if (sourceAccessor.Type.GetProperty(openNode.Name) != null)
            {
                return Expression.Property(sourceAccessor, openNode.Name);
            }

            // Property doesn't exists go for dynamic properties dictionary
            PropertyInfo prop = GetDynamicPropertyContainer(openNode);
            MemberExpression propertyAccessExpression = Expression.Property(sourceAccessor, prop.Name);
            IndexExpression readDictionaryIndexerExpression = Expression.Property(propertyAccessExpression,
                            DictionaryStringObjectIndexerName, Expression.Constant(openNode.Name));
            MethodCallExpression containsKeyExpression = Expression.Call(propertyAccessExpression,
                propertyAccessExpression.Type.GetMethod("ContainsKey"), Expression.Constant(openNode.Name));
            ConstantExpression nullExpression = Expression.Constant(null);

            if (QuerySettings.HandleNullPropagation == HandleNullPropagationOption.True)
            {
                var dynamicDictIsNotNull = Expression.NotEqual(propertyAccessExpression, Expression.Constant(null));
                var dynamicDictIsNotNullAndContainsKey = Expression.AndAlso(dynamicDictIsNotNull, containsKeyExpression);
                return Expression.Condition(
                    dynamicDictIsNotNullAndContainsKey,
                    readDictionaryIndexerExpression,
                    nullExpression);
            }
            else
            {
                return Expression.Condition(
                    containsKeyExpression,
                    readDictionaryIndexerExpression,
                    nullExpression);
            }
        }
    }
}
