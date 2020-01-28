// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NETCORE
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNet.OData.Test.Extensions;
#else
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.AspNet.OData;
#endif

namespace Microsoft.AspNet.OData.Test.Formatter
{
    public class UnknownPeopleController : ODataController
    {
        [EnableQuery]
        public IEnumerable<FormatterPerson> GetUnknownPeople()
        {
            return new FormatterPerson[]
            {
                new FormatterPerson { MyGuid = new Guid("f99080c0-2f9e-472e-8c72-1a8ecd9f902d"), PerId = 0, Age = 10, Name = "Asha", Order = new FormatterOrder() { OrderName = "FirstOrder", OrderAmount = 235342 }},
                new FormatterPerson { MyGuid = new Guid("f99080c0-2f9e-472e-8c72-1a8ecd9f902e"), PerId = 1, Age = 11, Name = null, Order = new FormatterOrder() { OrderName = "SecondOrder", OrderAmount = 123 }},
            };
        }

#if NETCORE
        public AspNetCore.Http.HttpResponse Post(FormatterPerson person)
        {
            return Request.CreateResponse(HttpStatusCode.Created, person);
        }
#else
        public System.Net.Http.HttpResponseMessage Post(FormatterPerson person)
        {
            return Request.CreateResponse(HttpStatusCode.Created, person);
        }
#endif
    }
}
