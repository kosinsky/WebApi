// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.UriParser;

namespace System.Web.OData.Query.Expressions
{
    internal class FlattenedVisitor
    {
        public bool HasNavigation { get; private set; }

        public void Visit(SingleValueNode node)
        {
            switch (node.Kind)
            {
                case QueryNodeKind.SingleNavigationNode:
                    Visit(node as SingleNavigationNode);
                    break;
                case QueryNodeKind.SingleValuePropertyAccess:
                    Visit(node as SingleValuePropertyAccessNode);
                    break;
                case QueryNodeKind.SingleComplexNode:
                    Visit(node as SingleComplexNode);
                    break;
                case QueryNodeKind.SingleValueFunctionCall:
                    Visit(node as SingleValueFunctionCallNode);
                    break;
            }
        }

        public void Visit(SingleNavigationNode node)
        {
            this.HasNavigation = true;
        }

        public void Visit(SingleComplexNode node)
        {
            this.HasNavigation = true;
        }

        public void Visit(SingleValuePropertyAccessNode node)
        {
            Visit(node.Source);
        }

        public void Visit(SingleValueFunctionCallNode node)
        {
            foreach (var arg in node.Parameters.OfType<SingleValueNode>())
            {
                Visit(arg);
            }
        }
    }
}
