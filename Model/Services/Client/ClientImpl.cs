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
    public class ClientImpl : IClient
    {
        private class InProgressTx
        {
            public readonly TaskCompletionSource<Dictionary<string, string>> tcs = new TaskCompletionSource<Dictionary<string, string>>();
            public readonly object mutex = new object();
            public readonly string txId;
            public Dictionary<string, string> result;
            public readonly ISet<string> acks = new HashSet<string>();
            public readonly ISet<string> nacks = new HashSet<string>();
            public readonly ISet<string> acceptors;
            public readonly ISet<string> shards;
            public bool hasSent;
            public bool hasFinished;

            public InProgressTx(ISet<string> shards, ISet<string> acceptors, string txId)
            {
                this.txId = txId;
                this.acceptors = acceptors;
                this.shards = shards;
                this.hasSent = false;
                this.hasFinished = false;
            }

            public bool HasMajority()
            {
                return this.acks.Count >= 1 + acceptors.Count / 2;
            }
        }

        private class AbortingTx
        {
            public readonly TaskCompletionSource<TxDetails> tcs = new TaskCompletionSource<TxDetails>();
            public readonly object mutex = new object();
            public readonly string reqId;
            public readonly string txId;
            public readonly string proposerId;
            public bool hasSent;
            public bool hasFinished;

            public AbortingTx(string reqId, string txId, string proposerId)
            {
                this.reqId = reqId;
                this.txId = txId;
                this.proposerId = proposerId;
                this.hasSent = false;
                this.hasFinished = false;
            }
        }

        private class FetchingTxStatus
        {
            public readonly TaskCompletionSource<TxStatus> tcs = new TaskCompletionSource<TxStatus>();
            public readonly object mutex = new object();
            public readonly string reqId;
            public readonly string txId;
            public readonly string proposerId;
            public bool hasSent;
            public bool hasFinished;
            
            public FetchingTxStatus(string reqId, string txId, string proposerId)
            {
                this.reqId = reqId;
                this.txId = txId;
                this.proposerId = proposerId;
                this.hasSent = false;
                this.hasFinished = false;
            }
        }

        private class TxDetails
        {
            public readonly IEnumerable<string> shardIds;

            public TxDetails(IEnumerable<string> shardIds)
            {
                this.shardIds = shardIds;
            }
        }

        private class CleaningTask
        {
            public readonly TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            public readonly object mutex = new object();
            public readonly string reqId;
            public readonly string txId;
            public readonly ISet<string> shardIDs;
            public readonly ISet<string> ack;
            public bool hasSent;
            public bool hasFinished;

            public CleaningTask(string reqId, string txId, ISet<string> shardIDs)
            {
                this.reqId = reqId;
                this.txId = txId;
                this.shardIDs = shardIDs;
                this.ack = new HashSet<string>();
                this.hasSent = false;
                this.hasFinished = false;
            }
        }

        private readonly IServiceLocator locator;
        private readonly INetworkBus bus;
        private readonly ITimer timer;
        private readonly Dictionary<string, InProgressTx> ongoingTXs = new Dictionary<string, InProgressTx>();
        private readonly Dictionary<string, AbortingTx> abortingTXs = new Dictionary<string, AbortingTx>();
        private readonly Dictionary<string, FetchingTxStatus> fetchingTXs = new Dictionary<string, FetchingTxStatus>();
        private readonly Dictionary<string, CleaningTask> cleaningTasks = new Dictionary<string, CleaningTask>();
        private readonly object mutex = new object();

        public ClientImpl(IServiceLocator locator, INetworkBus bus, ITimer timer)
        {
            this.locator = locator;
            this.bus = bus;
            this.timer = timer;
        }

        public Task<Dictionary<string, string>> ExecuteTx(string name, ISet<string> keys, Dictionary<string, string> args, int timeoutMs)
        {
            var txId = Guid.NewGuid().ToString();

            var shardIDs = new HashSet<string>(keys.Select(this.locator.GetShardIdByKey));

            var shardSubTXs = 
                from key in keys
                group key by this.locator.GetShardIdByKey(key)
                into shard 
                  let shardId = shard.Key 
                  let subTx = new ExecuteSubTxMessage(txId, name, new HashSet<string>(shard), shardIDs)
                select (shardId: shardId, subTx: subTx);
            
            var inProgressTx = new InProgressTx(
                shards: shardIDs, 
                acceptors: this.locator.GetAcceptorIDs(), 
                txId: txId
            );

            lock (this.mutex)
            {
                this.ongoingTXs.Add(txId, inProgressTx);
            }
            
            var argsMsg = new TxArgumentsMessage(txId, name, args, shardIDs);

            foreach (var acceptorId in inProgressTx.acceptors)
            {
                this.bus.SendArguments(acceptorId, argsMsg.Clone());
            }
            
            foreach (var (shardId, subTx) in shardSubTXs)
            {
                this.bus.ExecuteSubTx(shardId, subTx);
            }
            
            lock (inProgressTx.mutex)
            {
                inProgressTx.hasSent = true;
            }

            this.timer.SetTimeout(() =>
            {
                lock (this.mutex)
                {
                    lock (inProgressTx.mutex)
                    {
                        if (!inProgressTx.hasFinished)
                        {
                            inProgressTx.hasFinished = true;
                            if (this.ongoingTXs.ContainsKey(inProgressTx.txId))
                            {
                                this.ongoingTXs.Remove(inProgressTx.txId);
                            }
                            inProgressTx.tcs.SetException(new TxUnknownException(txId));
                        }
                    }
                }
            }, timeoutMs);
            
            return inProgressTx.tcs.Task;
        }

        public async Task AbortTx(string txId, int timeoutMs)
        {
            var proposerId = this.locator.GetRandomProposer();
            var reqId = Guid.NewGuid().ToString();
            
            var abortingTx = new AbortingTx(reqId, txId, proposerId);

            lock (this.mutex)
            {
                this.abortingTXs.Add(reqId, abortingTx);
            }
            
            this.bus.AbortTx(proposerId, new TxAbortMessage(reqId, txId));

            lock (abortingTx.mutex)
            {
                abortingTx.hasSent = true;
            }
            
            this.timer.SetTimeout(() =>
            {
                lock (this.mutex)
                {
                    lock (abortingTx.mutex)
                    {
                        if (!abortingTx.hasFinished)
                        {
                            abortingTx.hasFinished = true;
                            if (this.abortingTXs.ContainsKey(abortingTx.reqId))
                            {
                                this.abortingTXs.Remove(abortingTx.reqId);
                            }
                            abortingTx.tcs.SetException(new TxUnknownException(txId));
                        }
                    }
                }
            }, timeoutMs);

            var txDetails = await abortingTx.tcs.Task;
            
            var rollbackMsg = new RollbackSubTxMessage(txId);

            foreach (var shardId in txDetails.shardIds)
            {
                this.bus.RollbackTx(shardId, rollbackMsg.Clone());
            }
        }
        
        public Task<TxStatus> FetchTxStatus(string txId, int timeoutMs)
        {
            var proposerId = this.locator.GetRandomProposer();
            var reqId = Guid.NewGuid().ToString();
            
            var fetchingTx = new FetchingTxStatus(reqId, txId, proposerId);

            lock (this.mutex)
            {
                this.fetchingTXs.Add(reqId, fetchingTx);
            }
            
            this.bus.FetchTxStatus(proposerId, new FetchTxStatusMessage(reqId, txId));

            lock (fetchingTx.mutex)
            {
                fetchingTx.hasSent = true;
            }
            
            this.timer.SetTimeout(() =>
            {
                lock (this.mutex)
                {
                    lock (fetchingTx.mutex)
                    {
                        if (!fetchingTx.hasFinished)
                        {
                            fetchingTx.hasFinished = true;
                            if (this.fetchingTXs.ContainsKey(fetchingTx.reqId))
                            {
                                this.fetchingTXs.Remove(fetchingTx.reqId);
                            }
                            fetchingTx.tcs.SetException(new SomeException());
                        }
                    }
                }
            }, timeoutMs);

            return fetchingTx.tcs.Task;
        }

        public async Task MarkCommitted(string txId, Dictionary<string, Dictionary<string, string>> keyValueUpdateByShard, int timeoutMs)
        {
            var reqId = Guid.NewGuid().ToString();
            
            var cleaningTask = new CleaningTask(reqId, txId, new HashSet<string>(keyValueUpdateByShard.Keys));

            lock (this.mutex)
            {
                this.cleaningTasks.Add(reqId, cleaningTask);
            }

            foreach (var shardId in keyValueUpdateByShard.Keys)
            {
                this.bus.MarkSubTxCommitted(shardId, new MarkSubTxCommittedMessage(reqId, txId, keyValueUpdateByShard[shardId]));
            }

            lock (cleaningTask.mutex)
            {
                cleaningTask.hasSent = true;
            }
            
            this.timer.SetTimeout(() =>
            {
                lock (this.mutex)
                {
                    lock (cleaningTask.mutex)
                    {
                        if (!cleaningTask.hasFinished)
                        {
                            cleaningTask.hasFinished = true;
                            if (this.cleaningTasks.ContainsKey(cleaningTask.reqId))
                            {
                                this.cleaningTasks.Remove(cleaningTask.reqId);
                            }
                            cleaningTask.tcs.SetException(new SomeException());
                        }
                    }
                }
            }, timeoutMs);

            await cleaningTask.tcs.Task;
            
            this.bus.RmTx(this.locator.GetRandomProposer(), new RmTxMessage(txId));
        }

        public void OnSubTxMarkedCommitted(string shardId, SubTxMarkedComittedMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.cleaningTasks.ContainsKey(msg.ReqID)) return;
                
                var commitingTx = this.cleaningTasks[msg.ReqID];
                
                lock (commitingTx.mutex)
                {
                    if (!commitingTx.shardIDs.Contains(shardId)) return;

                    commitingTx.ack.Add(shardId);
                    
                    if (commitingTx.ack.Count != commitingTx.shardIDs.Count) return;
                    if (commitingTx.hasFinished) return;
                    
                    commitingTx.hasFinished = true;
                    this.cleaningTasks.Remove(commitingTx.reqId);
                }
                
                commitingTx.tcs.SetResult(true);
            }
        }
        
        public void OnExecutionCommitted(string acceptorId, TxComittedMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.ongoingTXs.ContainsKey(msg.TxID)) return;
                
                var ongoing = this.ongoingTXs[msg.TxID];
                
                lock (ongoing.mutex)
                {
                    if (!ongoing.acceptors.Contains(acceptorId)) return;

                    ongoing.acks.Add(acceptorId);
                    ongoing.result = msg.Result;

                    if (!ongoing.HasMajority()) return;

                    ongoing.hasFinished = true;
                    this.ongoingTXs.Remove(ongoing.txId);
                }
                
                ongoing.tcs.SetResult(ongoing.result);
            }
        }

        public void OnExecutionConflicted(string shardId, TxConflictMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.ongoingTXs.ContainsKey(msg.TxID)) return;
                
                var ongoing = this.ongoingTXs[msg.TxID];
                
                lock (ongoing.mutex)
                {
                    if (!ongoing.shards.Contains(shardId)) return;

                    ongoing.hasFinished = true;
                    this.ongoingTXs.Remove(ongoing.txId);
                }
                
                ongoing.tcs.SetException(new TxConflictException(ongoing.txId, msg.KeyBlockedByTX));
            }
        }

        public void OnAbortConfirmed(string proposerId, TxAbortedMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.abortingTXs.ContainsKey(msg.ReqID)) return;
                
                var aborting = this.abortingTXs[msg.ReqID];
                
                lock (aborting.mutex)
                {
                    if (aborting.proposerId != proposerId) return;

                    aborting.hasFinished = true;
                    this.abortingTXs.Remove(aborting.reqId);
                }
                
                aborting.tcs.SetResult(new TxDetails(msg.ShardIDs));
            }
        }
        
        public void OnAbortFailed(string proposerId, TxAlreadyCommittedMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.abortingTXs.ContainsKey(msg.ReqID)) return;
                
                var aborting = this.abortingTXs[msg.ReqID];
                
                lock (aborting.mutex)
                {
                    if (aborting.proposerId != proposerId) return;

                    aborting.hasFinished = true;
                    this.abortingTXs.Remove(aborting.reqId);
                }
                
                aborting.tcs.SetException(new AlreadyCommittedException(msg.TxID, msg.KeyValueUpdateByShard));
            }
        }

        public void OnTxStatusFetched(string proposerId, FetchedTxStatusMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.fetchingTXs.ContainsKey(msg.ReqID)) return;
                
                var fetching = this.fetchingTXs[msg.ReqID];
                
                lock (fetching.mutex)
                {
                    if (fetching.proposerId != proposerId) return;

                    fetching.hasFinished = true;
                    this.fetchingTXs.Remove(fetching.reqId);
                }
                
                fetching.tcs.SetResult(msg.Status);
            }
        }
    }
}