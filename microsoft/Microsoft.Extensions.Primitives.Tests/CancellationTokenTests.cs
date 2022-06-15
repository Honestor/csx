using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace Microsoft.Extensions.Primitives.Tests
{
    public class CancellationTokenTests
    {
        [Fact]
        public void Test1()
        {
            var tokenSource = new CancellationTokenSource();
            var callbackObj = "666";

            //注册一个委托当CancellationToken调用Cancel方法时,回调会被执行
            //如果此令牌已处于取消状态，则代理将立即同步运行。委托生成的任何异常都会从该方法调用中传播出去。ExecutionContext不会被捕获或流到回调的调用。
            tokenSource.Token.UnsafeRegister(obj =>
            {
                Trace.WriteLine(obj.ToString());
            }, callbackObj);

            tokenSource.Cancel();
        }

        [Fact]
        public void RouteTest()
        {
            
        }
    }
}
