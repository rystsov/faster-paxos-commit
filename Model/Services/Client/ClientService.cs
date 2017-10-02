using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Model.Infrastructure;
using Model.Services.Acceptor.Messages;
using Model.Services.Client.Exceptions;
using Model.Services.Client.Messages;
using Model.Services.Proposer.Messages;
using Model.Services.Shard.Messages;

namespace Model.Services.Client
{
    public class ClientService : IClient
    {
        private readonly IServiceLocator locator;
        private readonly INetworkBus bus;
        private readonly ITimer timer;

        public ClientService(IServiceLocator locator, INetworkBus bus, ITimer timer)
        {
            this.locator = locator;
            this.bus = bus;
            this.timer = timer;
        }

        public Task<Dictionary<string, string>> ExecuteTx(string name, ISet<string> keys, Dictionary<string, string> args, int timeoutMs)
        {
            var txId = Guid.NewGuid().ToString();

            var shardIDs = new HashSet<string>(keys.Select(this.locator.GetShardIdByKey));
            var acceptors = this.locator.GetAcceptorIDs();
            var result = new TaskCompletionSource<Dictionary<string, string>>();
            
            var localMutex = new object();
            var acks = new HashSet<string>();
            var hasFinished = false;

            var shardSubTXs = 
                from key in keys
                group key by this.locator.GetShardIdByKey(key)
                into shard 
                select (
                    shardId: shard.Key, 
                    subTx: new InitiateTxMessage(txId, name, new HashSet<string>(shard), shardIDs)
                );
            
            var argsMsg = new PrepareTxArgumentsMessage(this.bus.SelfID, txId, name, args, shardIDs);

            foreach (var acceptorId in acceptors)
            {
                this.bus.PrepareTxArguments(acceptorId, argsMsg.Clone());
            }
            
            foreach (var (shardId, subTx) in shardSubTXs)
            {
                this.bus.ExecuteSubTx(shardId, subTx);
            }
            
            var handler1 =  this.bus.WaitForExecutionAccepted(txId, (msg, acceptorId) =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return WaitStrategy.StopWaiting;
                    if (!acceptors.Contains(acceptorId)) return WaitStrategy.KeepWaiting;
                    
                    if (acks.Add(acceptorId))
                    {
                        if (acks.Count >= shardIDs.Count / 2 + 1)
                        {                            
                            result.SetResult(msg.Result);
                            return WaitStrategy.StopWaiting;
                        }                        
                    }
                    return WaitStrategy.KeepWaiting;
                }
            });
            
            var handler2 = this.bus.WaitForExecutionConflicted(txId, (msg, shardId) =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return WaitStrategy.StopWaiting;
                    if (!shardIDs.Contains(shardId)) return WaitStrategy.KeepWaiting;

                    hasFinished = true;
                    result.SetException(new TxConflictException(txId, msg.KeyBlockedByTX));
                    return WaitStrategy.StopWaiting;
                }
            });
            
            this.timer.SetTimeout(() =>
            {
                lock (localMutex)
                {
                    if (!hasFinished)
                    {
                        hasFinished = true;
                        result.SetException(new TxUnknownException(txId));
                        handler1.Dispose();
                        handler2.Dispose();
                    }
                }
            }, timeoutMs);
            
            return result.Task;
        }
        
        public async Task AbortTx(string txId, int timeoutMs)
        {
            var proposerId = this.locator.GetRandomProposer();
            var reqId = Guid.NewGuid().ToString();
            
            var result = new TaskCompletionSource<IEnumerable<string>>();
            var localMutex = new object();
            var hasFinished = false;

            this.bus.AbortTx(proposerId, new TxAbortMessage(reqId, txId));
            
            var handler1 = this.bus.WaitForAbortConfirmed(reqId, (msg, senderId) =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return WaitStrategy.StopWaiting;
                    if (senderId != proposerId) return WaitStrategy.KeepWaiting;

                    hasFinished = true;
                    result.SetResult(msg.ShardIDs);
                    return WaitStrategy.StopWaiting;
                }
            });

            var handler2 = this.bus.WaitForAbortFailed(reqId, (msg, senderId) =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return WaitStrategy.StopWaiting;
                    if (senderId != proposerId) return WaitStrategy.KeepWaiting;
                    
                    hasFinished = true;
                    result.SetException(new AlreadyCommittedException(msg.TxID, msg.KeyValueUpdateByShard));
                    return WaitStrategy.StopWaiting;
                }
            });

            this.timer.SetTimeout(() =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return;
                    hasFinished = true;
                    result.SetException(new TxUnknownException(txId));
                    handler1.Dispose();
                    handler2.Dispose();
                }
            }, timeoutMs);
            
            var shardIds = await result.Task;
            
            var rollbackMsg = new RollbackSubTxMessage(txId);

            foreach (var shardId in shardIds)
            {
                this.bus.RollbackTx(shardId, rollbackMsg.Clone());
            }
        }
        
        public Task<TxStatus> FetchTxStatus(string txId, int timeoutMs)
        {
            var proposerId = this.locator.GetRandomProposer();
            var reqId = Guid.NewGuid().ToString();
            
            var result = new TaskCompletionSource<TxStatus>();
            var localMutex = new object();
            var hasFinished = false;
            
            this.bus.FetchTxStatus(proposerId, new FetchTxStatusMessage(reqId, txId));

            // OPUS NASA
            
            var handler1 = this.bus.WaitForTxStatusFetched(reqId, (msg, senderId) =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return WaitStrategy.StopWaiting;
                    if (proposerId != senderId) return WaitStrategy.KeepWaiting;

                    hasFinished = true;
                
                    result.SetResult(msg.Status);
                    return WaitStrategy.StopWaiting;
                }
            });
            
            this.timer.SetTimeout(() =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return;
                    hasFinished = true;
                    result.SetException(new SomeException());
                    handler1.Dispose();
                }
            }, timeoutMs);

            return result.Task;
        }

        public async Task MarkCommitted(string txId, Dictionary<string, Dictionary<string, string>> keyValueUpdateByShard, int timeoutMs)
        {
            var reqId = Guid.NewGuid().ToString();
            
            var result = new TaskCompletionSource<bool>();
            var localMutex = new object();
            var shardIDs = new HashSet<string>(keyValueUpdateByShard.Keys);
            var acks = new HashSet<string>();
            var hasFinished = false;
            
            foreach (var shardId in keyValueUpdateByShard.Keys)
            {
                this.bus.MarkSubTxCommitted(shardId, new MarkTxComittedMessage(reqId, txId, keyValueUpdateByShard[shardId]));
            }
            
            var handler1 = this.bus.WaitForSubTxMarkedCommitted(reqId, (msg, shardId) =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return WaitStrategy.StopWaiting;
                    if (!shardIDs.Contains(shardId)) return WaitStrategy.KeepWaiting;
                    
                    acks.Add(shardId);
                    
                    if (acks.Count != shardIDs.Count) return WaitStrategy.KeepWaiting;
                    
                    hasFinished = true;
                    result.SetResult(true);
                    return WaitStrategy.StopWaiting;
                }
            });
            
            this.timer.SetTimeout(() =>
            {
                lock (localMutex)
                {
                    if (hasFinished) return;
                    hasFinished = true;
                    result.SetException(new SomeException());
                    handler1.Dispose();
                }
            }, timeoutMs);

            await result.Task;
            
            this.bus.RmTx(this.locator.GetRandomProposer(), new RmTxMessage(txId));
        }
    }
}