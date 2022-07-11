using Microsoft.AspNetCore.Builder;
using System;

namespace Framework.AspNetCore.Connections
{
    
    public sealed class ConnectionEndpointRouteBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _endpointConventionBuilder;

        internal ConnectionEndpointRouteBuilder(IEndpointConventionBuilder endpointConventionBuilder)
        {
            _endpointConventionBuilder = endpointConventionBuilder;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            _endpointConventionBuilder.Add(convention);
        }
    }
}
