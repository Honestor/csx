using Dapper;
using Framework.Test;
using Framework.Uow;
using Oracle.ManagedDataAccess.Client;
using System;
using Xunit;

namespace Framework.Data.Oralce.Tests
{
    public class OperateTest : OracleTest
    {
        [Fact]
        public void ConnectTest()
        {
            //OracleConfiguration.OracleDataSources.Add("orcl", "(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=172.18.100.231)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)))");
            var conn = new OracleConnection("User Id=wpsm;Password=wpsm;Data Source=172.18.100.231:1521/orcl;");
            conn.Open();
            var result=conn.Query<Test>("select * from HeartRateIndex");
        }
    }

    public class Test
    { 
        public int Id { get; set; }

        public string RoomNumber { get; set; }
    }
}
