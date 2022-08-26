using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.ElasticSearch.Nest
{
    /// <summary>
    /// 索引管理
    /// </summary>
    public class IndexingManager<T> where T:class,new()
    {
        private IClientProvider _clientProvider;

        public IndexingManager(IClientProvider clientProvider)
        {
            _clientProvider = clientProvider;
        }

        public async Task<bool> AddAsync(T entity,string? indexName =null)
        {
            var client = await _clientProvider.GetClientAsync();
            var response = await client.IndexAsync(entity, i => i.Index(indexName??nameof(T)));
            return response.IsValid;
        }

        /// <summary>
        /// 批量插入 复杂的批量插入建议使用BulkAll配合BulkAllObservable
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public async Task<bool> BulkAsync(IEnumerable<T> entities, string? indexName = null)
        {
            var client = await _clientProvider.GetClientAsync();
            var asyncBulkIndexResponse = await client.BulkAsync(b => b
                .Index(indexName ?? nameof(T))
                .IndexMany(entities)
            );
            return asyncBulkIndexResponse.IsValid;
        }

        public async Task BulkAllAsync(IEnumerable<T> entities, string? indexName = null, Func<BulkResponseItemBase, T, bool>? retryPredicate = null, Action<BulkResponseItemBase, T>? documentLost = null)
        {
            var seenPages = 0;
            var observableBulk =await BulkAllAsync(indexName??nameof(T), entities, r => r
                .ContinueAfterDroppedDocuments()
                .DroppedDocumentCallback((bulkResponseItem, item) =>
                {
                    Console.WriteLine($"{bulkResponseItem},{item}");
                })
            );
            observableBulk.Wait(TimeSpan.FromMinutes(5), b => Interlocked.Increment(ref seenPages));
            var waitHandle = new ManualResetEvent(false);
            ExceptionDispatchInfo? exceptionDispatchInfo = null;

            var observer = new BulkAllObserver(
                onNext: response =>
                {

                },
                onError: exception =>
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
                    waitHandle.Set();
                },
                onCompleted: () => waitHandle.Set());

            observableBulk.Subscribe(observer);

            waitHandle.WaitOne();

            exceptionDispatchInfo?.Throw();
        }

        private async Task<BulkAllObservable<T>> BulkAllAsync(string index,IEnumerable<T> documents,Func<BulkAllDescriptor<T>, BulkAllDescriptor<T>>? selector = null)
        {
            var client = await _clientProvider.GetClientAsync();
            selector = selector ?? (s => s);
            var observableBulk = client.BulkAll(documents, f => selector(f
                .MaxDegreeOfParallelism(Environment.ProcessorCount)
                .BackOffTime(TimeSpan.FromSeconds(10))
                .BackOffRetries(2)
                .Size(1000)
                .RefreshOnCompleted()
                .Index(index)
            ));
            return observableBulk;
        }
    }
}
