using Dapper;
using Framework.Test;
using Framework.Uow;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Framework.Data.Oralce.Tests
{
    public class OperateTest : OracleTest
    {
        [Fact]
        public async Task ConnectTest()
        {
            using (var uow = UnitOfWorkManager.Begin(new UnitOfWorkOptions() { }))
            {
                var conn = await DbProvider.GetConnectionAsync();
                var id2 = Thread.CurrentThread.ManagedThreadId;
                var id = Thread.CurrentThread.ManagedThreadId;
                var result = await conn.QueryAsync<Test>("select * from HeartRateIndex");
                var id1 = Thread.CurrentThread.ManagedThreadId;
            }
        }
    }

    public class Test
    { 
        public int Id { get; set; }

        public string RoomNumber { get; set; }
    }
}
