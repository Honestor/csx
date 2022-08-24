using CanalSharp.Connections;
using CanalSharp.Protocol;
using Framework.Core.Dependency;
using Framework.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Canal
{
    public class CanalConsumer:ISingleton
    {
        private CanalConnectionFactory _connectionFactory;
        private ILogger<CanalConsumer> _logger;

        public CanalConsumer(CanalConnectionFactory connectionFactory, ILogger<CanalConsumer> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task ConsumeSingleAsync(string filter, Action<List<string>> callback, CancellationToken cancellationToken = default)
        {
            var connection = await _connectionFactory.CreateSingleAsync();
            await connection.SubscribeAsync(filter);
            await connection.RollbackAsync(0);
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                var message = await connection.GetWithoutAckAsync(1024);
                if (message.Id == -1 || message.Entries.Count <= 0)
                {
                    await Task.Delay(300);
                    continue;
                }
                var changeRows = await SolveAsync(connection, message);
                if (changeRows.Count > 0)
                    callback?.Invoke(changeRows);
            }
        }

        private async Task<List<string>> SolveAsync(SimpleCanalConnection connection, Message message)
        {
            var details = new List<string>();
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
                        foreach (var rowData in rowChange.RowDatas)
                        {
                            if (eventType == EventType.Delete)
                            {
                                var result = GetJsonString(rowData.BeforeColumns.ToList(), entry, eventType.ToString());
                                if (!string.IsNullOrEmpty(result))
                                {
                                    details.Add(result);
                                }
                            }
                            if (eventType == EventType.Insert)
                            {
                                var result = GetJsonString(rowData.AfterColumns.ToList(), entry, eventType.ToString());
                                if (!string.IsNullOrEmpty(result))
                                {
                                    details.Add(result);
                                }
                            }
                            if (eventType == EventType.Update)
                            {
                                var result = GetJsonString(rowData.AfterColumns.ToList(), entry, eventType.ToString());
                                if (!string.IsNullOrEmpty(result))
                                {
                                    details.Add(result);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"数据映射失败,信息:{ex.Message},堆栈:{ex.StackTrace}");
            }
            finally
            {
                await connection.AckAsync(message.Id);
            }
            return details;
        }

        private static string GetJsonString(List<Column> columns, Entry entry,string eventType)
        {
            var marksTypes = new List<string>() { "varchar", "char", "date", "datetime", "timestamp", "text", "year" };
            var str = "{";
            str += $"\"BinLogFileName\":\"{entry.Header.LogfileName}\",";
            str += $"\"DatabaseName\":\"{entry.Header.SchemaName}\",";
            str += $"\"TableName\":\"{entry.Header.TableName}\",";
            str += $"\"LogfileOffset\":\"{entry.Header.LogfileOffset}\",";
            str += $"\"EventType\":\"{eventType}\",";
            str += "\"Data\":{";
           
            for (var i=0;i<columns.Count;i++)
            {
                var column = columns[i];
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
                if(i!= columns.Count-1)
                    str += $",";
                else
                    return str += "}";
            }
            return str;
        }
    }
}
