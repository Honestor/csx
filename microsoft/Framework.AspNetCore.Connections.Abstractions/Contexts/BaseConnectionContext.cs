// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections.Abstractions
{
    public abstract class BaseConnectionContext : IAsyncDisposable
    {
        /// <summary>
        /// ����Id
        /// </summary>
        public abstract string ConnectionId { get; set; }

        /// <summary>
        /// Feature����
        /// </summary>
        public abstract IFeatureCollection Features { get; }

        /// <summary>
        /// �ͻ������ӹرմ���token
        /// </summary>
        public virtual CancellationToken ConnectionClosed { get; set; }

        /// <summary>
        /// ��������
        /// </summary>
        public abstract void Abort();

        /// <summary>
        /// ��������ӦΪ�쳣
        /// </summary>
        /// <param name="abortReason"></param>
        public abstract void Abort(ConnectionAbortedException abortReason);

        public virtual ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
