using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Model.Infrastructure;
using Model.Infrastructure.ShardStorage;
using Model.Services.Acceptor.Messages;
using Model.Services.Client.Messages;
using Model.Services.Shard.Messages;

namespace Model.Services.Shard
{
    public class ShardService
    {
        private class AbortException : Exception {}
        
        // hyfen.net/memex
        private readonly IShardStorage storage;
        private readonly INetworkBus bus;
        private readonly IServiceLocator locator;
        private readonly ITimer timer;

        public ShardService(IShardStorage storage, INetworkBus bus, IServiceLocator locator, ITimer timer)
        {
            this.storage = storage;
            this.bus = bus;
            this.locator = locator;
            this.timer = timer;
        }
        
        public async Task InitiateTx(string clientId, InitiateTxMessage msg, int timeoutMs)
        {
            Dictionary<string, string> values;
            try
            {
                values = await this.storage.ReadAndBlock(msg.TxID, msg.TxName, msg.ShardIDs, msg.Keys);
            }
            catch (AlreadyBlockedException ex)
            {
                this.bus.NotifyExecutionConflicted(clientId, new ExecutionConflictedMessage(msg.TxID, ex.BlockedKeyTxPairs));
                return;
            }
            
            var acceptors = this.locator.GetAcceptorIDs();
                
            var result = new TaskCompletionSource<Dictionary<string, string>>();
            var localMutex = new object();
            var acks = new HashSet<string>();
            var hasFinished = false;

            var confirmation = new CommitTxMessage(this.bus.SelfID, msg.TxID, values);
            foreach (var acceptorID in acceptors)
            {
                this.bus.CommitTx(acceptorID, confirmation.Clone());
            }
            
            var handler1 = this.bus.WaitForSubTxAccepted(msg.TxID, (subTxAccepted, acceptorId) =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return WaitStrategy.StopWaiting;
                    if (!acceptors.Contains(acceptorId)) return WaitStrategy.KeepWaiting;

                    acks.Add(acceptorId);

                    if (acks.Count < 1 + acceptors.Count / 2) return WaitStrategy.KeepWaiting;

                    hasFinished = true;
                    result.SetResult(subTxAccepted.KeyValueUpdate);
                    return WaitStrategy.StopWaiting;
                }
            });
                
            this.timer.SetTimeout(() =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return;
                    hasFinished = true;
                    result.SetException(new AbortException());
                    handler1.Dispose();
                }
            }, timeoutMs);

            Dictionary<string, string> update;

            try
            {
                update = await result.Task;
            }
            catch (AbortException)
            {
                return;
            }
            
            await this.storage.UpdateAndUnblock(msg.TxID, update);
        }

        public async Task MarkTxComitted(string clientId, MarkTxComittedMessage msg)
        {
            await this.storage.UpdateAndUnblock(msg.TxID, msg.KeyValueUpdate);
            
            this.bus.NotifySubTxMarkedCommitted(clientId, msg.ReqID);
        }
        
        public async Task RollbackTx(string _, RollbackSubTxMessage msg)
        {
            await this.storage.Unblock(msg.TxID);
        }
    }
}