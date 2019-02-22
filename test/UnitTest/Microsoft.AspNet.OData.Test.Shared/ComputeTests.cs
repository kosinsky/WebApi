// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NETCORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Test.Abstraction;
using Microsoft.AspNet.OData.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
#else
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Test.Abstraction;
using Microsoft.AspNet.OData.Test.Common;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
#endif
namespace Microsoft.AspNet.OData.Test
{
    public class ComputeTests
    {
        private const string AcceptJsonFullMetadata = "application/json;odata.metadata=full";
        private const string AcceptJson = "application/json";

        [Theory]
        [InlineData("$compute=ID add ID as DoubleID&$select=ID,DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID&$filter=ID gt 0")]
        [InlineData("$compute=ID add ID as DoubleID&$filter=DoubleID gt 0")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=ID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=DoubleID, ID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=ID, DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=DoubleID desc, ID desc")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=ID desc, DoubleID desc")]
        [InlineData("$compute=ID add ID as DoubleID&$filter=DoubleID gt 0&$orderby=DoubleID")]
        public async Task DollarCompute_Works(string clause)
        {
            // Arrange
            var uri = $"/odata/SelectExpandTestCustomers?{clause}";

            // Act
            HttpResponseMessage response = await GetResponse(uri, AcceptJsonFullMetadata);

            // Assert
            Assert.NotNull(response);
            JObject result = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(result["value"][0]["ID"]);
            Assert.NotNull(result["value"][0]["DoubleID"]);
        }

        [Theory]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$select=ID,DoubleID")]
        [InlineData("$apply=compute(ID add ID as DoubleID)")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$filter=ID gt 0")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$filter=DoubleID gt 0")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$orderby=ID")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$orderby=DoubleID")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$orderby=DoubleID, ID")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$orderby=ID, DoubleID")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$orderby=DoubleID desc, ID desc")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$orderby=ID desc, DoubleID desc")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$filter=DoubleID gt 0&$orderby=DoubleID")]
        public async Task ApplyCompute_Works(string clause)
        {
            // Arrange
            var uri = $"/odata/SelectExpandTestCustomers?{clause}";

            // Act
            HttpResponseMessage response = await GetResponse(uri, AcceptJsonFullMetadata);

            // Assert
            Assert.NotNull(response);
            JObject result = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(result["value"][0]["ID"]);
            Assert.NotNull(result["value"][0]["DoubleID"]);
        }

        [Theory]
        [InlineData("$apply=aggregate(ID with sum as TotalID, $count as Count)&$select=TotalID")]
        [InlineData("$apply=aggregate(ID with sum as TotalID, $count as Count)/compute(Count as Count2)&$select=TotalID")]
        [InlineData("$apply=groupby((ID))/compute(ID as TotalID)&$select=TotalID")]
        [InlineData("$apply=groupby((ID), aggregate($count as Count))/compute(ID as TotalID)&$select=TotalID")]
        public async Task ApplyAndSelect_Works(string clause)
        {
            // Arrange
            var uri = $"/odata/SelectExpandTestCustomers?{clause}";

            // Act
            HttpResponseMessage response = await GetResponse(uri, AcceptJsonFullMetadata);

            // Assert
            Assert.NotNull(response);
            JObject result = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(result["value"][0]["TotalID"]);
            Assert.Null(result["value"][0]["Count"]);
            Assert.Null(result["value"][0]["Count2"]);
        }

        [Theory]
        [InlineData("$apply=aggregate(ID with sum as TotalID, $count as Count)&$compute=TotalID as TotalID2")]
        [InlineData("$apply=aggregate(ID with sum as TotalID, $count as Count)&$compute=TotalID as TotalID2, Count as Count2&$select=TotalID,Count,TotalID2")]
        public async Task ComputeAfterApply_Works(string clause)
        {
            // Arrange
            var uri = $"/odata/SelectExpandTestCustomers?{clause}";

            // Act
            HttpResponseMessage response = await GetResponse(uri, AcceptJsonFullMetadata);

            // Assert
            Assert.NotNull(response);
            JObject result = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(result["value"][0]["TotalID"]);
            Assert.NotNull(result["value"][0]["Count"]);
            Assert.NotNull(result["value"][0]["TotalID2"]);
            Assert.Null(result["value"][0]["Count2"]);
        }


        private Task<HttpResponseMessage> GetResponse(string uri, string acceptHeader)
        {
            var controllers = new[] {
                typeof(SelectExpandTestCustomersController),
            };

            var server = TestServerFactory.Create(controllers, (config) =>
            {
#if NETFX
                config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
#endif
                config.Count().Filter().OrderBy().Expand().MaxTop(null).Select();
                config.MapODataServiceRoute("odata", "odata", GetModel());
#if NETCORE
                config.MapRoute("api", "api/{controller}", new { controller = "NonODataSelectExpandTestCustomers", action="Get" });
#else
                config.Routes.MapHttpRoute("api", "api/{controller}", new { controller = "NonODataSelectExpandTestCustomers" });
#endif
                config.EnableDependencyInjection();
            });

            HttpClient client = TestServerFactory.CreateClient(server);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost" + uri);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
#if NETFX
            request.SetConfiguration(server.Configuration);
#endif
            return client.SendAsync(request);
        }

        private IEdmModel GetModel()
        {
            ODataConventionModelBuilder builder = ODataConventionModelBuilderFactory.Create();
            builder.EntitySet<SelectExpandTestCustomer>("SelectExpandTestCustomers");
            builder.EntitySet<SelectExpandTestOrder>("SelectExpandTestOrders");
            builder.Ignore<SelectExpandTestSpecialCustomer>();
            builder.Ignore<SelectExpandTestSpecialOrder>();
            return builder.GetEdmModel();
        }
    }
}
