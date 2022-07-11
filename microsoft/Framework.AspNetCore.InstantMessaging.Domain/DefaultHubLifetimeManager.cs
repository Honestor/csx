using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public class DefaultHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
    {
        private readonly HubConnectionStore _connections = new HubConnectionStore();
        private readonly HubGroupList _groups = new HubGroupList();
        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            _connections.Add(connection);
            return Task.CompletedTask;
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, include: null, state: null, cancellationToken);
        }

        private Task SendToAllConnections(string methodName, object?[]? args, Func<HubConnectionContext, object?, bool>? include, object? state = null, CancellationToken cancellationToken = default)
        {
            List<Task>? tasks = null;
            SerializedHubMessage? message = null;
            foreach (var connection in _connections)
            {
                //过滤上下文
                if (include != null && !include(connection, state))
                {
                    continue;
                }

                if (message == null)
                {
                    message = CreateSerializedInvocationMessage(methodName, args);
                }

                var task = connection.WriteAsync(message, cancellationToken);

                if (!task.IsCompletedSuccessfully)
                {
                    if (tasks == null)
                    {
                        tasks = new List<Task>();
                    }

                    tasks.Add(task.AsTask());
                }
            }

            if (tasks == null)
            {
                return Task.CompletedTask;
            }

            // Some connections are slow
            return Task.WhenAll(tasks);
        }
        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            _groups.Add(connection, groupName);

            return Task.CompletedTask;
        }

        public override Task SendGroupAsync(string groupName, string methodName, object?[]? args, CancellationToken cancellationToken = default)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var group = _groups[groupName];
            if (group != null)
            {
                // Can't optimize for sending to a single connection in a group because
                // group might be modified inbetween checking and sending
                List<Task>? tasks = null;
                SerializedHubMessage? message = null;
                SendToGroupConnections(methodName, args, group, null, null, ref tasks, ref message, cancellationToken);

                if (tasks != null)
                {
                    return Task.WhenAll(tasks);
                }
            }

            return Task.CompletedTask;
        }

        private void SendToGroupConnections(string methodName, object?[]? args, ConcurrentDictionary<string, HubConnectionContext> connections, Func<HubConnectionContext, object?, bool>? include, object? state, ref List<Task>? tasks, ref SerializedHubMessage? message, CancellationToken cancellationToken)
        {
            // foreach over ConcurrentDictionary avoids allocating an enumerator
            foreach (var connection in connections)
            {
                if (include != null && !include(connection.Value, state))
                {
                    continue;
                }

                if (message == null)
                {
                    message = CreateSerializedInvocationMessage(methodName, args);
                }

                var task = connection.Value.WriteAsync(message, cancellationToken);

                if (!task.IsCompletedSuccessfully)
                {
                    if (tasks == null)
                    {
                        tasks = new List<Task>();
                    }

                    tasks.Add(task.AsTask());
                }
            }
        }

        private SerializedHubMessage CreateSerializedInvocationMessage(string methodName, object?[]? args)
        {
            return new SerializedHubMessage(CreateInvocationMessage(methodName, args));
        }

        private HubMessage CreateInvocationMessage(string methodName, object?[]? args)
        {
            return new InvocationMessage(methodName, args);
        }
    }
}
