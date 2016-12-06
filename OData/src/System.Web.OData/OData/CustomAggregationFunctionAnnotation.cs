using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.Web.OData
{
    /// <summary>
    /// Custom aggregation method
    /// </summary>
    public class CustomAggregationFunctionAnnotation
    {
        /// <summary>
        /// .Ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="methods"></param>
        public CustomAggregationFunctionAnnotation(string name, Dictionary<Type, MethodInfo> methods)
        {
            this.Name = name;
            this.Methods = methods;
        }

        /// <summary>
        /// Gets name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets methods dictionary.
        /// </summary>
        public Dictionary<Type, MethodInfo> Methods {get; private set;}
    }
}
