using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class Program
    {
        private static int _flag;
        public static void Main(string[] args)
        {
             var r1=Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Parse("/Home/Index");
            var r2 = Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Parse("/hello/{name:alpha}");
            var matcher = new DfaMatcherFactory().CreateMatcher(_endpointDataSource);
            Console.ReadKey();
        }
    }

    public class Test
    { }
}
