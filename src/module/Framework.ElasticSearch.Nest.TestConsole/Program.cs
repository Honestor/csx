using Framework.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Framework.ElasticSearch.Nest.TestConsole
{
    internal class Program
    {
        static Program()
        {
            new ServiceCollection()
                .UseCore()
                .UseNest()
                .LoadModules();
        }

        static void Main(string[] args)
        {
            var manager = ApplicationConfiguration.Current.Provider.GetRequiredService<IndexingManager<Logs>>();
            for (var i= 0;i< 1000;i++)
            {
                var stop = Stopwatch.StartNew();
                stop.Start();
                var result= manager.AddAsync(new Logs() { Content = Guid.NewGuid().ToString(), Level = Guid.NewGuid().ToString() }, "logs").ConfigureAwait(false).GetAwaiter().GetResult();
                stop.Stop();
                if (!result)
                    Console.WriteLine("数据添加失败,索引asdasdasddddddddddddddddddddddddddddddddddddddddddddddddddddddddd" + i);
                else
                    Console.WriteLine($"数据添加成功,索引:{i},耗时:{stop.ElapsedMilliseconds}");
            }
            Console.ReadKey();
        }
    }

    public class Logs
    { 
        public string Content { get; set; }

        public string Level { get; set; }
    }
}