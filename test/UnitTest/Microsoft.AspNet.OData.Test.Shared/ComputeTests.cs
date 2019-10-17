// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.


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
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.AspNet.OData.Test.Abstraction;
using Microsoft.AspNet.OData.Test.Common;
using Microsoft.AspNet.OData.Test.Query.Validators;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Microsoft.OData.UriParser;
using Xunit.Abstractions;

#if NETCORE
using Microsoft.AspNetCore.Builder;
#else
using System.Web.Http;

#endif

namespace Microsoft.AspNet.OData.Test
{
    public static class BoundFunctions
    {
        private static object locker = new object();
        private static bool registred = false;

        public static void RegisterFunctions(IEdmTypeReference enumRef)
        {
            if (!registred)
            {
                lock (locker)
                {
                    if (!registred)
                    {
                        RegisterFunction(typeof(BoundFunctions), nameof(BoundFunctions.GetBestOrders));
                        RegisterFunction(typeof(SelectExpandTestCustomerWithCustom), nameof(SelectExpandTestCustomerWithCustom.GetInstanceBestOrders));
                        RegisterFunction(typeof(SelectExpandTestCustomerWithCustom), nameof(SelectExpandTestCustomerWithCustom.GetInt));
                        RegisterFunction(typeof(SelectExpandTestCustomerWithCustom), nameof(SelectExpandTestCustomerWithCustom.ConvertEnum));

                        var s = new FunctionSignatureWithReturnType(EdmLibHelpers.GetEdmPrimitiveTypeReferenceOrNull(typeof(double?)), enumRef);
                        ODataUriFunctions.AddCustomUriFunction("convert_enum", s, typeof(BoundFunctions).GetMethods().Where(m => m.Name == nameof(BoundFunctions.ConvertEnum)).First());

                        registred = true;
                    }
                }
            }
        }

        private static void RegisterFunction(Type type, string methodName)
        {
            var best = type.GetMethods().Where(m => m.Name == methodName).First();
            UriFunctionsBinder.BindUriFunctionName($"Default.{methodName}", best);
        }

        public static IQueryable<SelectExpandTestOrder> GetBestOrders()
        {
            return Enumerable.Range(1, 1).Select(i => new SelectExpandTestOrder()
            {
                ID = i
            }).AsQueryable();
        }

        public static int ConvertEnum(TestEnum e)
        {
            return (int)e;
        }
    }

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

        // TODO: Support expression on top of bound functions

        [Theory]
        [InlineData("SelectExpandTestCustomers", "$compute=Default.GetBestOrders() as Best")]
        [InlineData("SelectExpandTestCustomerWithCustoms", "$compute=Default.GetInstanceBestOrders() as Best")]
        public async Task DollarCompute_SuppportsBoundFunctions(string entitySet, string clause)
        {
            // Arrange
            var uri = $"/odata/{entitySet}?{clause}";

            // Act
            HttpResponseMessage response = await GetResponse(uri, AcceptJsonFullMetadata);

            // Assert
            Assert.NotNull(response);
            JObject result = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("1", result["value"][0]["Best"][0]["ID"]);
        }


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
        [InlineData("$compute=ID add ID as DoubleID&$select=ID,DoubleID&$filter=Orders/any(o:o/Amount gt 0)")]
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
        [InlineData("$compute=Default.GetInt(p=2) as DoubleID")]
        [InlineData("$compute=convert_enum('One') as DoubleID")]
        [InlineData("$compute=Default.ConvertEnum(p='One') as DoubleID")]
        [InlineData("$compute=Default.ConvertEnum(p='One') as DoubleID&$filter=Default.ConvertEnum(p='One') ge 0")]
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

        [Theory]
        [InlineData("$expand=PreviousCustomer($select=ID, ID2;$compute=ID as ID2)")]
        public async Task ComputeInSingleExpand_Throws(string clause)
        {
            // Arrange
            var uri = $"/odata/SelectExpandTestCustomers?{clause}";

            // Act
            HttpResponseMessage response = await GetResponse(uri, AcceptJsonFullMetadata);

            // Assert
            Assert.NotNull(response);
            string result = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Contains("$apply/$compute not supported for single property PreviousCustomer", result);
        }

        [Theory]
        [InlineData("$expand=Orders($select=ID, ID2;$compute=ID as ID2)")]
        [InlineData("$expand=Orders($select=ID, ID2;$compute=ID as ID2,ID as ID3)")]
        [InlineData("$expand=Orders($select=ID, ID2;$compute='x' as ID2;$filter=ID2 ne null)")]
        public async Task ComputeInExpand_Works_WithCustom(string clause)
        {
            // Arrange
            var uri = $"/odata/SelectExpandTestCustomerWithCustoms?{clause}";

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
                typeof(TC),
                typeof(TestEnum)
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
            var set = builder.EntitySet<SelectExpandTestCustomer>("SelectExpandTestCustomers");
            builder.EntitySet<SelectExpandTestOrder>("SelectExpandTestOrders");
            var set2 = builder.EntitySet<SelectExpandTestCustomerWithCustom>("SelectExpandTestCustomerWithCustoms");

            builder.Ignore<SelectExpandTestSpecialCustomer>();
            builder.Ignore<SelectExpandTestSpecialOrder>();

            set.EntityType.Function("GetBestOrders").ReturnsCollectionFromEntitySet<SelectExpandTestOrder>("SelectExpandTestOrders");
            set2.EntityType.Function("GetInstanceBestOrders").ReturnsCollectionFromEntitySet<SelectExpandTestOrder>("SelectExpandTestOrders");
            set2.EntityType.Function("GetInt").Returns<int>().Parameter<int>("p");
            set2.EntityType.Function("ConvertEnum").Returns<int>().Parameter<TestEnum>("p");
            builder.EnumType<TestEnum>();

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

            BoundFunctions.RegisterFunctions(model.SchemaElements.OfType<EdmEnumType>().First(e => e.Name == "TestEnum").ToEdmTypeReference(false));

            return model;
        }
    }
}
