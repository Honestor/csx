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
            //OracleConfiguration.OracleDataSources.Add("orcl", "(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=172.18.100.231)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))");
            var conn = new OracleConnection("User Id=wpsm;Password=wpsm;Data Source=172.18.100.231:1521/orcl;");
            var id2 = Thread.CurrentThread.ManagedThreadId;
            await conn.OpenAsync();
            var id = Thread.CurrentThread.ManagedThreadId;
            var result= await conn.QueryAsync<Test>("select * from HeartRateIndex");
            var id1 = Thread.CurrentThread.ManagedThreadId;

        }
    }

    public class Test
    { 
        public int Id { get; set; }

        public string RoomNumber { get; set; }
    }
}
