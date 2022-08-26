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


        static async Task Main(string[] args)
        {
            await BulkAllAsync();
            Console.ReadKey();
        }

        /// <summary>
        /// 异步单条添加
        /// </summary>
        /// <returns></returns>
        static async Task SingleAddAsync()
        {

            var _manager = ApplicationConfiguration.Current.Provider.GetRequiredService<IndexingManager<Logs>>();
            var stop = Stopwatch.StartNew();
            stop.Start();
            for (var i = 0; i < 100; i++)
            {

                await _manager.AddAsync(Logs.Get(i), "logs");
                Console.WriteLine($"当前索引:{i}");

            }
            stop.Stop();
            Console.WriteLine($"异步数据添加成功,耗时:{stop.ElapsedMilliseconds}");
        }

        /// <summary>
        /// 异步批量添加
        /// </summary>
        /// <returns></returns>
        static async Task BulkAsync()
        {
            var _manager = ApplicationConfiguration.Current.Provider.GetRequiredService<IndexingManager<Logs>>();
            var stop = Stopwatch.StartNew();
            stop.Start();
            var bulkData = new List<Logs>();
            for (var i = 0; i < 10000; i++)
            {
                bulkData.Add(Logs.Get(i));
            }
            await _manager.BulkAsync(bulkData, "logs");
            stop.Stop();
         
            Console.WriteLine($"异步数据添加成功,耗时:{stop.ElapsedMilliseconds}");
        }

        static async Task BulkAllAsync()
        {
            var _manager = ApplicationConfiguration.Current.Provider.GetRequiredService<IndexingManager<Logs>>();
          
            var bulkData = new List<Logs>();
            for (var i = 0; i < 10000; i++)
            {
                bulkData.Add(Logs.Get(i));
            }
            var stop = Stopwatch.StartNew();
            stop.Start();
            await _manager.BulkAllAsync(bulkData, "logs");
            stop.Stop();
            Console.WriteLine($"异步数据添加成功,耗时:{stop.ElapsedMilliseconds}");
        }
    }

    public class Logs
    { 
        public float Price { get; set; }

        public static Logs Get(float price)
        {
            return new Logs() { Price = price };
        }
    }
}