// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NETCORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Test.Abstraction;
using Microsoft.AspNet.OData.Test.Common;
using Microsoft.AspNet.OData.Test.Query.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.OData;
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
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Test.Abstraction;
using Microsoft.AspNet.OData.Test.Common;
using Microsoft.AspNet.OData.Test.Query.Validators;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
#endif
namespace Microsoft.AspNet.OData.Test
{
    public class ComputeTest : ComputeTests<SelectExpandTestCustomersController, SelectExpandTestCustomerWithCustomsController>
    {

    }

    public class ComputeTestWithPaging : ComputeTests<SelectExpandTestCustomersWithPagingController, SelectExpandTestCustomerWithCustomsWithPagingController>
    {

    }

    public abstract class ComputeTests<T, TC>
    {
        private const string AcceptJsonFullMetadata = "application/json;odata.metadata=full";
        private const string AcceptJson = "application/json";

        [Theory]
        [InlineData("$compute=ID add ID as DoubleID&$select=ID,DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID&$filter=ID gt 0")]
        [InlineData("$compute=ID add ID as DoubleID&$filter=DoubleID gt 0")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=ID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=ID&$top=10")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=DoubleID&$top=10")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=DoubleID, ID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=DoubleID, ID&$top=10")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=ID, DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=ID, DoubleID&$top=10")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=DoubleID desc, ID desc")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=ID desc, DoubleID desc")]
        [InlineData("$compute=ID add ID as DoubleID&$filter=DoubleID gt 0&$orderby=DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID&$filter=DoubleID gt 0&$orderby=DoubleID&$top=10")]
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
        [InlineData("$filter=ID gt 42&$compute=ID add ID as DoubleID&$select=ID,DoubleID&$expand=PreviousCustomer($select=ID)")]
        public async Task DollarCompute_WorksWithExtraExpand(string clause)
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
            Assert.NotNull(result["value"][0]["PreviousCustomer"]);
            Assert.NotNull(result["value"][0]["PreviousCustomer"]["ID"]);
        }

        [Theory]
        [InlineData("$compute=ID add ID as DoubleID&$select=ID,DoubleID,TestField")]
        [InlineData("$apply=compute(ID add ID as DoubleID)&$select=ID,DoubleID,TestField")]
        [InlineData("$compute=ID add ID as DoubleID")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=Name")]
        [InlineData("$compute=ID add ID as DoubleID&$orderby=Name desc")]
        [InlineData("$apply=compute(ID add ID as DoubleID)")]
        //[InlineData("$apply=compute(ID add ID as DoubleID2)&$compute=DoubleID2 as DoubleID")] // TODO: Support $compute after $apply
        public async Task DollarCompute_WorksWithCustomFields(string clause)
        {
            // Arrange
            var uri = $"/odata/SelectExpandTestCustomerWithCustoms?{clause}";

            // Act
            HttpResponseMessage response = await GetResponse(uri, AcceptJsonFullMetadata);

            // Assert
            Assert.NotNull(response);
            var res = await response.Content.ReadAsStringAsync();
            JObject result = JObject.Parse(res);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(result["value"][0]["ID"]);
            Assert.NotNull(result["value"][0]["DoubleID"]);
            Assert.NotNull(result["value"][0]["TestField"]);
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

        [Theory]
        [InlineData("$expand=Orders($select=ID, ID2;$compute=ID as ID2)")]
        [InlineData("$expand=Orders($select=ID, ID2;$compute=ID as ID2,ID as ID3)")]
        [InlineData("$expand=Orders($select=ID, ID2;$compute=ID as ID2;$filter=ID2 eq 24)")]
        public async Task ComputeInExpand_Works(string clause)
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
            var orders = result["value"][0]["Orders"];
            Assert.NotNull(orders[0]["ID"]);
            Assert.NotNull(orders[0]["ID2"]);
            Assert.Null(orders[0]["ID3"]);
        }

        [Fact]
        public void CanTurnOffValidationForFilter()
        {
            ODataValidationSettings settings = new ODataValidationSettings() { AllowedFunctions = AllowedFunctions.AllDateTimeFunctions };
            ODataQueryContext context = ValidationTestHelper.CreateCustomerContext();
            ComputeQueryOption option = new ComputeQueryOption("substring(Name,8,1) as NewProp", context);

            ExceptionAssert.Throws<ODataException>(() =>
                option.Validate(settings),
                "Function 'substring' is not allowed. To allow it, set the 'AllowedFunctions' property on EnableQueryAttribute or QueryValidationSettings.");

            option.Validator = null;
            ExceptionAssert.DoesNotThrow(() => option.Validate(settings));
        }

        private Task<HttpResponseMessage> GetResponse(string uri, string acceptHeader)
        {
            var controllers = new[] {
                typeof(T),
                typeof(TC)
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
            builder.EntitySet<SelectExpandTestCustomerWithCustom>("SelectExpandTestCustomerWithCustoms");

            builder.Ignore<SelectExpandTestSpecialCustomer>();
            builder.Ignore<SelectExpandTestSpecialOrder>();
            var model = builder.GetEdmModel();

            var entityType = model.EntityContainer.EntitySets().First(e => e.Name == "SelectExpandTestCustomerWithCustoms").EntityType() as EdmEntityType;
            var containerProperty = typeof(SelectExpandTestCustomerWithCustom).GetProperty(nameof(SelectExpandTestCustomerWithCustom.Custom));
            var clrProperty = containerProperty.PropertyType.GetProperty(nameof(CustomFields.TestField));


            var edmProperty = entityType.AddStructuralProperty("TestField", EdmPrimitiveTypeKind.String, true);
            model.SetAnnotationValue(edmProperty, new ClrPropertyInfoAnnotation(clrProperty)
            {
                PropertiesPath = new List<PropertyInfo>()
                        {
                            containerProperty
                        }
            });

            var modelBound = model.GetAnnotationValue<ModelBoundQuerySettings>(entityType) ?? new ModelBoundQuerySettings();
            modelBound.DefaultSelectType = SelectExpandType.Automatic;
            modelBound.MaxTop = null; // Ensure that system wide settings are respected
            model.SetAnnotationValue(entityType, modelBound);


            return model;
        }
    }
}
