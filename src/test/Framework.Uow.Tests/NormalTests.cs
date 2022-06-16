using Dapper;
using Framework.Core.Data;
using Framework.Core.Dependency;
using Framework.Test;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Framework.Uow.Tests
{
    public class NormalTests: UowOfWorkTestBase
    {
        /// <summary>
        /// 嵌套工作单元测试
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Test1()
        {
            using (var uow = UnitOfWorkManager.Begin(new UnitOfWorkOptions() { IsTransactional = true }))
            {
                using (var uow1 = UnitOfWorkManager.Begin(new UnitOfWorkOptions() { IsTransactional = true }))
                {
                    try
                    {
                        var id1 = Thread.CurrentThread.ManagedThreadId;
                        await TestRepository.Update("222");
                        var id2 = Thread.CurrentThread.ManagedThreadId;
                        await uow1.CompleteAsync();
                        var id3 = Thread.CurrentThread.ManagedThreadId;
                    }
                    catch 
                    {
                        await uow1.RollbackAsync();
                    }
                }

                try
                {
                    await TestRepository.Update("333");
                    await uow.CompleteAsync();
                }
                catch
                {
                    await uow.RollbackAsync();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Fact]
        public void Test2()
        {
            Parallel.For(0, 100, async (index) =>
            {
                using (var uow = UnitOfWorkManager.Begin(new UnitOfWorkOptions() { IsTransactional = true }))
                {
                    try
                    {
                        await TestRepository.Insert(index.ToString(),Thread.CurrentThread.ManagedThreadId.ToString());
                        await uow.CompleteAsync();
                    }
                    catch
                    {
                        await uow.RollbackAsync();
                    }
                }
            });

            Thread.Sleep(15000);
        }
    }
}
