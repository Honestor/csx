using CanalSharp.Protocol;
using Framework.Core.Configurations;
using Framework.Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Framework.Canal.ConsoleTest 
{
    internal class Program
    {
        static Program()
        {
            new ServiceCollection()
                .UseCore()
                .UseSerilog()
                .UseCanal()
                .LoadModules();
        }

        static async Task Main(string[] args)
        {
            var connectionFactory = ApplicationConfiguration.Current.Provider.GetRequiredService<CanalConnectionFactory>();
            var conn=await connectionFactory.CreateSingleAsync();
            await conn.RollbackAsync(0);
            while (true)
            {
                var msg = await conn.GetAsync(1024);
                PrintEntry(msg.Entries);
                await Task.Delay(300);
            }
        }

        private static void PrintEntry(List<Entry> entries)
        {
            var _logger = ApplicationConfiguration.Current.Provider.GetRequiredService<ILogger<Program>>();
            foreach (var entry in entries)
            {
                if (entry.EntryType == EntryType.Transactionbegin || entry.EntryType == EntryType.Transactionend)
                {
                    continue;
                }

                RowChange rowChange = null;

                try
                {
                    rowChange = RowChange.Parser.ParseFrom(entry.StoreValue);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                }

                if (rowChange != null)
                {
                    EventType eventType = rowChange.EventType;

                    _logger.LogInformation(
                        $"================> binlog[{entry.Header.LogfileName}:{entry.Header.LogfileOffset}] , name[{entry.Header.SchemaName},{entry.Header.TableName}] , eventType :{eventType}");

                    foreach (var rowData in rowChange.RowDatas)
                    {
                        if (eventType == EventType.Delete)
                        {
                            PrintColumn(rowData.BeforeColumns.ToList());
                        }
                        else if (eventType == EventType.Insert)
                        {
                            PrintColumn(rowData.AfterColumns.ToList());
                        }
                        else
                        {
                            _logger.LogInformation("-------> before");
                            PrintColumn(rowData.BeforeColumns.ToList());
                            _logger.LogInformation("-------> after");
                            PrintColumn(rowData.AfterColumns.ToList());
                        }
                    }
                }
            }
        }

        private static void PrintColumn(List<Column> columns)
        {
            foreach (var column in columns)
            {
                Console.WriteLine($"{column.Name} ： {column.Value}  update=  {column.Updated}");
            }
        }
    }
}