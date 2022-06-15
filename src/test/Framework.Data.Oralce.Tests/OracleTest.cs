using Framework.Core.Data;
using Framework.Dapper;
using Framework.Test;
using Framework.Uow;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using Xunit;

namespace Framework.Data.Oralce.Tests
{
    public class OracleTest : TestBase
    {
        protected IUnitOfWorkManager UnitOfWorkManager;
        protected IDbProvider DbProvider;
        public OracleTest()
        {
            UnitOfWorkManager = ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            DbProvider = ServiceProvider.GetRequiredService<IDbProvider>();
        }

        protected override void LoadModules()
        {
            ApplicationConfiguration
                .UseUnitOfWork()
                .UseOracel()
                .UseDapper();
        }
    }
}
