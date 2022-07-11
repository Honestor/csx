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
        /// 连接Id
        /// </summary>
        public abstract string ConnectionId { get; set; }

        /// <summary>
        /// Feature集合
        /// </summary>
        public abstract IFeatureCollection Features { get; }

        /// <summary>
        /// 客户端连接关闭触发token
        /// </summary>
        public virtual CancellationToken ConnectionClosed { get; set; }

        /// <summary>
        /// 挂起连接
        /// </summary>
        public abstract void Abort();

        /// <summary>
        /// 挂起连接应为异常
        /// </summary>
        /// <param name="abortReason"></param>
        public abstract void Abort(ConnectionAbortedException abortReason);

        public virtual ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
