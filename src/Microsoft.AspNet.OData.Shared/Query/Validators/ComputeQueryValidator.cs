using Microsoft.AspNet.OData.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.OData.Query.Validators
{
    /// <summary>
    /// Represents a validator used to validate a <see cref="ComputeQueryOption" /> based on the <see cref="ODataValidationSettings"/>.
    /// </summary>
    /// <remarks>
    /// Please note this class is not thread safe.
    /// </remarks>
    public class ComputeQueryValidator : ExpressionQueryValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeQueryValidator" /> class based on
        /// the <see cref="DefaultQuerySettings" />.
        /// </summary>
        /// <param name="defaultQuerySettings">The <see cref="DefaultQuerySettings" />.</param>
        public ComputeQueryValidator(DefaultQuerySettings defaultQuerySettings) : base(defaultQuerySettings)
        {
        }

        /// <summary>
        /// Validates a <see cref="ComputeQueryOption" />.
        /// </summary>
        /// <param name="computeQueryOption">The $compute query.</param>
        /// <param name="settings">The validation settings.</param>
        /// <remarks>
        /// Please note this method is not thread safe.
        /// </remarks>
        public virtual void Validate(ComputeQueryOption computeQueryOption, ODataValidationSettings settings)
        {
            if (computeQueryOption == null)
            {
                throw Error.ArgumentNull(nameof(computeQueryOption));
            }

            if (settings == null)
            {
                throw Error.ArgumentNull(nameof(settings));
            }

            if (computeQueryOption.Context.Path != null)
            {
                Property = computeQueryOption.Context.TargetProperty;
                StructuredType = computeQueryOption.Context.TargetStructuredType;
            }

            Validate(computeQueryOption.ComputeClause, settings, computeQueryOption.Context.Model);
        }

        /// <summary>
        /// Validates a <see cref="ComputeClause" />.
        /// </summary>
        /// <param name="computeClause">The <see cref="ComputeClause" />.</param>
        /// <param name="settings">The validation settings.</param>
        /// <param name="model">The EdmModel.</param>
        /// <remarks>
        /// Please note this method is not thread safe.
        /// </remarks>
        public virtual void Validate(ComputeClause computeClause, ODataValidationSettings settings, IEdmModel model)
        {
            CurrentAnyAllExpressionDepth = 0;
            CurrentNodeCount = 0;
            base.Model = model;

            foreach (ComputeExpression computeITem in computeClause.ComputedItems)
            {
                ValidateQueryNode(computeITem.Expression, settings);
            }
        }

        internal static ComputeQueryValidator GetComputeQueryValidator(ODataQueryContext context)
        {
            if (context == null)
            {
                return new ComputeQueryValidator(new DefaultQuerySettings());
            }

            return context.RequestContainer == null
                ? new ComputeQueryValidator(context.DefaultQuerySettings)
                : context.RequestContainer.GetRequiredService<ComputeQueryValidator>();
        }
    }
}
