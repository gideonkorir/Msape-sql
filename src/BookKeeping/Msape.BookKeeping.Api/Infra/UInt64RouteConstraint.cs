using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api.Infra
{
    public class UInt64RouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if(values.TryGetValue(routeKey, out object value))
            {
                var valueAsString = Convert.ToString(value);
                if (ulong.TryParse(valueAsString, out var _))
                    return true;
            }
            return false;
        }
    }
}
