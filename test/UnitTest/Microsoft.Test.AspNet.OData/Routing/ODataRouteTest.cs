﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NETCORE
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
#else
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;
using Microsoft.AspNet.OData.Routing;
using Xunit;
#endif

namespace Microsoft.Test.AspNet.OData.Routing
{
    public class ODataRouteTest
    {
        [Fact]
        public void GenerateLinkDirectly_DoesNotReturnNull_IfHelperRequestHasNoConfiguration()
        {
            // Arrange
            var config = RoutingConfigurationFactory.Create();
            ODataRoute odataRoute = CreateRoute(config, "prefix");

            // Act
            var virtualPathData = odataRoute.GenerateLinkDirectly("odataPath");

            // Assert
            Assert.True(odataRoute.CanGenerateDirectLink);
            Assert.NotNull(virtualPathData);
#if NETCORE
            Assert.Equal("/prefix/odataPath", virtualPathData.VirtualPath);
#else
            Assert.Equal("prefix/odataPath", virtualPathData.VirtualPath);
#endif
        }

        [Fact]
        public void CanGenerateDirectLink_IsFalse_IfRouteTemplateHasParameterInPrefix()
        {
            // Arrange && Act
            var config = RoutingConfigurationFactory.Create();
            ODataRoute odataRoute = CreateRoute(config, "{prefix}");

            // Assert
            Assert.False(odataRoute.CanGenerateDirectLink);
        }

        [Fact]
        public void GenerateLinkDirectly_DoesNotReturnNull_IfRoutePrefixIsNull()
        {
            // Arrange
            var config = RoutingConfigurationFactory.Create();
            ODataRoute odataRoute = CreateRoute(config, routePrefix: null);

            // Act
            var virtualPathData = odataRoute.GenerateLinkDirectly("odataPath");

            // Assert
            Assert.True(odataRoute.CanGenerateDirectLink);
            Assert.NotNull(virtualPathData);

#if NETCORE
            Assert.Equal("/odataPath", virtualPathData.VirtualPath);
#else
            Assert.Equal("odataPath", virtualPathData.VirtualPath);
#endif
        }

        [Fact]
        public void GetVirtualPath_CanGenerateDirectLinkIsTrue_IfRoutePrefixIsNull()
        {
            // Arrange
            var config = RoutingConfigurationFactory.CreateWithRoute("http://localhost/vpath");
            var request = RequestFactory.Create(HttpMethod.Get, "http://localhost/vpath/prefix/Customers", config);
            ODataRoute odataRoute = CreateRoute(config, routePrefix: null);

            // Act
            var virtualPathData = GetVirtualPath(odataRoute, request,
                new Dictionary<string,object> { { "odataPath", "odataPath" }, { "httproute", true } });

            // Assert
            Assert.True(odataRoute.CanGenerateDirectLink);
            Assert.NotNull(virtualPathData);
#if NETCORE
            Assert.Equal("/odataPath", virtualPathData.VirtualPath);
#else
            Assert.Equal("odataPath", virtualPathData.VirtualPath);
#endif
        }

        [Fact]
        public void ODataVersionConstraint_DefaultIsRelaxedValueIsTrue()
        {
            var config = RoutingConfigurationFactory.Create();
            ODataRoute odataRoute = CreateRoute(config, routePrefix: null);
            Assert.True(((ODataVersionConstraint)odataRoute.Constraints[ODataRouteConstants.VersionConstraintName]).IsRelaxedMatch);
        }

        [Fact]
        public void ODataVersionConstraint_DefaultValue()
        {
            var config = RoutingConfigurationFactory.Create();
            ODataRoute odataRoute = CreateRoute(config, routePrefix: null);
            Assert.True(((ODataVersionConstraint)odataRoute.Constraints[ODataRouteConstants.VersionConstraintName]).IsRelaxedMatch);
        }

        [Theory]
        [InlineData("")]
        [InlineData("odataPath")]
        [InlineData("Customers('$&+,/:;=?@ <>#%{}|\\^~[]` ')")]
        public void GetVirtualPath_MatchesHttpRoute(string odataPath)
        {
            // Arrange
            var config = RoutingConfigurationFactory.CreateWithRoute("http://localhost/vpath");
            var request = RequestFactory.Create(HttpMethod.Get, "http://localhost/vpath/prefix/Customers", config);

            var httpRoute = CreateHttpRoute(config, "prefix/{*odataPath}");
            ODataRoute odataRoute = CreateRoute(config, "prefix");

            // Act
            var expectedVirtualPathData = GetVirtualPath(httpRoute, request, new Dictionary<string, object> { { "odataPath", odataPath }, { "httproute", true } });
            var actualVirtualPathData = GetVirtualPath(odataRoute, request, new Dictionary<string, object> { { "odataPath", odataPath }, { "httproute", true } });

            // Test that the link generated by ODataRoute matches the one generated by HttpRoute
            Assert.Equal(expectedVirtualPathData.VirtualPath, actualVirtualPathData.VirtualPath);
        }

#if NETCORE
        private ODataRoute CreateRoute(IRouteBuilder builder, string routePrefix)
        {
            // Get constraint resolver.
            IInlineConstraintResolver inlineConstraintResolver = builder
                .ServiceProvider
                .GetRequiredService<IInlineConstraintResolver>();

            return new ODataRoute(builder.DefaultHandler, null, routePrefix, null, inlineConstraintResolver);
        }

        private IRouter CreateHttpRoute(IRouteBuilder builder, string routeTemplate)
        {
            // Get constraint resolver.
            IInlineConstraintResolver inlineConstraintResolver = builder
                .ServiceProvider
                .GetRequiredService<IInlineConstraintResolver>();

            return new Route(builder.DefaultHandler, routeTemplate, inlineConstraintResolver);
        }

        private VirtualPathData GetVirtualPath(IRouter odataRoute, HttpRequest request, IDictionary<string, object> values)
        {
            VirtualPathContext context = new VirtualPathContext(request.HttpContext, null, new RouteValueDictionary(values));
            return odataRoute.GetVirtualPath(context);
        }
#else
        private ODataRoute CreateRoute(HttpConfiguration config, string routePrefix)
        {
            return new ODataRoute(routePrefix, pathConstraint: null);
        }

        private IHttpRoute CreateHttpRoute(HttpConfiguration config, string routeTemplate)
        {
            return config.Routes.CreateRoute(routeTemplate, defaults: null, constraints: null);
        }

        private IHttpVirtualPathData GetVirtualPath(IHttpRoute odataRoute, HttpRequestMessage request, IDictionary<string, object> values)
        {
            return odataRoute.GetVirtualPath(
                request,
                new HttpRouteValueDictionary(values));
        }
#endif
    }
}