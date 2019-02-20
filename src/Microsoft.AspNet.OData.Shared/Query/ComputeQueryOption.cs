using Microsoft.AspNet.OData.Adapters;
using Microsoft.AspNet.OData.Common;
using Microsoft.AspNet.OData.Interfaces;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.OData.Query
{
    /// <summary>
    /// This defines a $compute OData query option for querying.
    /// </summary>
    public class ComputeQueryOption
    {
        private ComputeClause _computeClause;
        private ODataQueryOptionParser _queryOptionParser;
        private readonly IWebApiAssembliesResolver _assembliesResolver;

        /// <summary>
        /// Initialize a new instance of <see cref="ComputeQueryOption"/> based on the raw $compute value and
        /// an EdmModel from <see cref="ODataQueryContext"/>.        /// </summary>
        /// <param name="rawValue"></param>
        /// <param name="context"></param>
        /// <param name="queryOptionParser"></param>
        public ComputeQueryOption(string rawValue, ODataQueryContext context, ODataQueryOptionParser queryOptionParser)
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
            _queryOptionParser = queryOptionParser;

            // The IWebApiAssembliesResolver service is internal and can only be injected by WebApi.
            // This code path may be used in cases when the service container is not available
            // and the service container is available but may not contain an instance of IWebApiAssembliesResolver.
            _assembliesResolver = Context.RequestContainer?.GetService<IWebApiAssembliesResolver>() ?? WebApiAssembliesResolver.Default;

        }

        /// <summary>
        ///  Gets the given <see cref="ODataQueryContext"/>.
        /// </summary>
        public ODataQueryContext Context { get; private set; }

        /// <summary>
        ///  Gets the raw $compute value.
        /// </summary>
        public string RawValue { get; private set; }

        ///// <summary>
        ///// Gets or sets the OrderBy Query Validator.
        ///// </summary>
        //public OrderByQueryValidator Validator { get; set; }

        /// <summary>
        /// Gets the parsed <see cref="ComputeClause"/> for this query option.
        /// </summary>
        public ComputeClause ComputeClause
        {
            get
            {
                if (_computeClause == null)
                {
                    _computeClause = _queryOptionParser.ParseCompute();
                }

                return _computeClause;
            }
        }

        internal IQueryable ApplyTo(IQueryable query, ODataQuerySettings querySettings)
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

            ComputeClause computeClause = ComputeClause;
            Contract.Assert(computeClause != null);

            ODataQuerySettings updatedSettings = Context.UpdateQuerySettings(querySettings, query);

            var binder = new ComputeBinder(updatedSettings, _assembliesResolver, Context.ElementClrType, Context.Model, computeClause.ComputedItems);
            query = binder.Bind(query);
            //this.ResultClrType = binder.ResultClrType;

            return query;

        }

        internal void Validate(ODataValidationSettings validationSettings)
        {
            if (validationSettings == null)
            {
                throw Error.ArgumentNull("validationSettings");
            }
            if (ComputeClause == null)
            {
                throw Error.ArgumentNull("ComputeClause");
            }
            // TODO: Add real validation logic here
        }
    }
}
