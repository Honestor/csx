using CanalSharp.Protocol;
using Framework.Core.Configurations;
using Framework.Json;
using Framework.Serilog;
using Framework.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Framework.Canal.ConsoleTest
{
    internal class Program
    {
        static Program()
        {
            new ServiceCollection()
                .UseCore()
                .UseSerilog()
                .UseJson()
                .UseTiming()
                .UseCanal()
                .UseApplication()
                .LoadModules();
        }

        static async Task Main(string[] args)
        {
            var consumer = ApplicationConfiguration.Current.Provider.GetRequiredService<CanalConsumer<Test>>();
            await consumer.ConsumeSingleAsync("quzhou_baseasset.test");
            Console.ReadKey();
        }

        public class Test
        { 
            public string id { get; set; }

            public string object_id { get; set; }

            public string room_name { get; set; }

            public DateTime? add_time { get; set; }

            public long? heart_rate { get; set; }
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
                            var result = GetJsonString(rowData.BeforeColumns.ToList());
                            if (!string.IsNullOrEmpty(result))
                            { 
                                
                            }
                        }
                        else if (eventType == EventType.Insert)
                        {
                            var result = GetJsonString(rowData.AfterColumns.ToList());
                            if (!string.IsNullOrEmpty(result))
                            {

                            }
                        }
                        else
                        {
                            var result = GetJsonString(rowData.BeforeColumns.ToList());
                            if (!string.IsNullOrEmpty(result))
                            {

                            }
                        }
                    }
                }
            }
        }

        private static string GetJsonString(List<Column> columns)
        {
            var marksTypes = new List<string>() { "varchar","char","date","datetime", "timestamp","text","year" };
            var str = "{";

            foreach (var column in columns)
            {
                str += $"\"{column.Name}\":";
                if (!string.IsNullOrEmpty(column.MysqlType))
                {
                    if (column.IsNull)
                    {
                        str += $"null,";
                        continue;
                    }

                    var markFlag = false;
                    marksTypes.ForEach(type =>
                    {
                        if (column.MysqlType.Contains(type))
                            markFlag = true;
                    });
                    if (markFlag)
                    {
                        str += $"\"{column.Value}\"";
                    }
                    else
                    {
                        str += $"{column.Value}";
                    }
                }
                str += $",";
            }
            if (str.Length > 1)
            {
                var result=str.Substring(0, str.Length - 1);
                return result += "}";
            }
            else {
                return string.Empty; ;
            }
        }
    }
}