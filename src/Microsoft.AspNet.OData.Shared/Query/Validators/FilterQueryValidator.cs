// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Microsoft.AspNet.OData.Common;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.AspNet.OData.Query.Validators
{
    /// <summary>
    /// Represents a validator used to validate a <see cref="FilterQueryOption" /> based on the <see cref="ODataValidationSettings"/>.
    /// </summary>
    /// <remarks>
    /// Please note this class is not thread safe.
    /// </remarks>
    public class FilterQueryValidator :ExpressionQueryValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterQueryValidator" /> class based on
        /// the <see cref="DefaultQuerySettings" />.
        /// </summary>
        /// <param name="defaultQuerySettings">The <see cref="DefaultQuerySettings" />.</param>
        public FilterQueryValidator(DefaultQuerySettings defaultQuerySettings)
            :base(defaultQuerySettings)
        {
        }

        /// <summary>
        /// Validates a <see cref="FilterQueryOption" />.
        /// </summary>
        /// <param name="filterQueryOption">The $filter query.</param>
        /// <param name="settings">The validation settings.</param>
        /// <remarks>
        /// Please note this method is not thread safe.
        /// </remarks>
        public virtual void Validate(FilterQueryOption filterQueryOption, ODataValidationSettings settings)
        {
            if (filterQueryOption == null)
            {
                throw Error.ArgumentNull("filterQueryOption");
            }

            if (settings == null)
            {
                throw Error.ArgumentNull("settings");
            }

            if (filterQueryOption.Context.Path != null)
            {
                Property = filterQueryOption.Context.TargetProperty;
                StructuredType = filterQueryOption.Context.TargetStructuredType;
            }

            Validate(filterQueryOption.FilterClause, settings, filterQueryOption.Context.Model);
        }

        /// <summary>
        /// Validates a <see cref="FilterClause" />.
        /// </summary>
        /// <param name="filterClause">The <see cref="FilterClause" />.</param>
        /// <param name="settings">The validation settings.</param>
        /// <param name="model">The EdmModel.</param>
        /// <remarks>
        /// Please note this method is not thread safe.
        /// </remarks>
        public virtual void Validate(FilterClause filterClause, ODataValidationSettings settings, IEdmModel model)
        {
            CurrentAnyAllExpressionDepth = 0;
            CurrentNodeCount = 0;
            base.Model = model;

            ValidateQueryNode(filterClause.Expression, settings);
        }

        internal virtual void Validate(IEdmProperty property, IEdmStructuredType structuredType,
            FilterClause filterClause, ODataValidationSettings settings, IEdmModel model)
        {
            base.Property = property;
            base.StructuredType = structuredType;
            Validate(filterClause, settings, model);
        }

        internal static FilterQueryValidator GetFilterQueryValidator(ODataQueryContext context)
        {
            if (context == null)
            {
                return new FilterQueryValidator(new DefaultQuerySettings());
            }

            return context.RequestContainer == null
                ? new FilterQueryValidator(context.DefaultQuerySettings)
                : context.RequestContainer.GetRequiredService<FilterQueryValidator>();
        }
    }
}