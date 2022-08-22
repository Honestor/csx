using CanalSharp.Connections;
using CanalSharp.Protocol;
using Framework.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.Canal
{
    public class CanalConsumer<T> where T:class,new()
    {
        private CanalConnectionFactory _connectionFactory;
        private ILogger<CanalConsumer<T>> _logger;
        private IJsonSerializer _jsonSerializer;

        public CanalConsumer(CanalConnectionFactory connectionFactory, ILogger<CanalConsumer<T>> logger, IJsonSerializer jsonSerializer)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// 单机消费 not zookeeper集群
        /// </summary>
        /// <returns></returns>
        public async Task ConsumeSingleAsync(string filter = ".*\\..*")
        {
            var connection = await _connectionFactory.CreateSingleAsync();
            await connection.SubscribeAsync(filter);
            while (true)
            {
                await SolveAsync(connection, await connection.GetWithoutAckAsync(1024));
                await Task.Delay(300);
            }
        }

        private async Task<List<TableChangeDetail<T>>> SolveAsync(SimpleCanalConnection connection, Message message) 
        {
            var details = new List<TableChangeDetail<T>>();
            try
            {
                foreach (var entry in message.Entries)
                {
                    if (entry.EntryType == EntryType.Transactionbegin || entry.EntryType == EntryType.Transactionend)
                    {
                        continue;
                    }

                    var rowChange = RowChange.Parser.ParseFrom(entry.StoreValue);

                    if (rowChange != null)
                    {
                        EventType eventType = rowChange.EventType;

                        var detail = new TableChangeDetail<T>()
                        {
                            BinLogFileName = entry.Header.LogfileName,
                            DatabaseName = entry.Header.SchemaName,
                            TableName = entry.Header.TableName,
                            EventType = eventType.ToString()
                        };

                        foreach (var rowData in rowChange.RowDatas)
                        {
                            if (eventType == EventType.Delete)
                            {
                                //PrintColumn(rowData.BeforeColumns.ToList());
                            }
                            else if (eventType == EventType.Insert)
                            {
                                var result = GetJsonString(rowData.AfterColumns.ToList());
                                if (!string.IsNullOrEmpty(result))
                                {
                                    detail.Data = _jsonSerializer.Deserialize<T>(result);
                                }
                            }
                            else
                            {
                                //_logger.LogInformation("-------> before");
                                //PrintColumn(rowData.BeforeColumns.ToList());
                                //_logger.LogInformation("-------> after");
                                //PrintColumn(rowData.AfterColumns.ToList());
                            }
                        }

                        details.Add(detail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"数据映射失败,信息:{ex.Message},堆栈:{ex.StackTrace}");
                await connection.RollbackAsync(message.Id);
                details.Clear();
            }
            if(details.Count>0)
                await connection.AckAsync(message.Id);
            return details;
        }

        private static string GetJsonString(List<Column> columns)
        {
            var marksTypes = new List<string>() { "varchar", "char", "date", "datetime", "timestamp", "text", "year" };
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
                var result = str.Substring(0, str.Length - 1);
                return result += "}";
            }
            else
            {
                return string.Empty; ;
            }
        }
    }
}
