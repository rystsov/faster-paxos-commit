using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
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
            public readonly string txId;
            public readonly string proposerId;
            public bool hasSent;
            public bool hasFinished;

            public AbortingTx(string txId, string proposerId)
            {
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

        private readonly IServiceLocator locator;
        private readonly INetworkBus bus;
        private readonly ITimer timer;
        private readonly Dictionary<string, InProgressTx> ongoingTXs = new Dictionary<string, InProgressTx>();
        private readonly Dictionary<string, AbortingTx> abortingTXs = new Dictionary<string, AbortingTx>();
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
            
            var outByShards = new Dictionary<string, SubTxMessage>();
            foreach (var key in keys)
            {
                var shardId = this.locator.GetShardIdByKey(key);
                if (!outByShards.ContainsKey(shardId))
                {
                    outByShards.Add(shardId, new SubTxMessage(txId, name));
                }
                outByShards[shardId].Keys.Add(key);
            }

            var inProgressTx = new InProgressTx(
                shards: new HashSet<string>(outByShards.Keys), 
                acceptors: this.locator.GetAcceptorIDs(), 
                txId: txId
            );

            lock (this.mutex)
            {
                this.ongoingTXs.Add(txId, inProgressTx);
            }
            
            var argsMsg = new TxArgumentsMessage(txId, args);

            foreach (var acceptorId in inProgressTx.acceptors)
            {
                this.bus.SendArguments(acceptorId, argsMsg.Clone());
            }
            
            foreach (var shardId in outByShards.Keys)
            {
                this.bus.ExecuteSubTx(shardId, outByShards[shardId].Clone());
            }
            
            lock (inProgressTx.mutex)
            {
                inProgressTx.hasSent = true;
            }

            this.timer.SetTimeout(() =>
            {
                lock (inProgressTx.mutex)
                {
                    if (!inProgressTx.hasFinished)
                    {
                        inProgressTx.hasFinished = true;
                        inProgressTx.tcs.SetException(new TxUnknownException(txId));
                    }
                }
            }, timeoutMs);
            
            return inProgressTx.tcs.Task;
        }

        public async Task AbortTx(string txId, int timeoutMs)
        {
            var proposerId = this.locator.GetRandomProposer();
            
            var abortingTx = new AbortingTx(txId, proposerId);

            lock (this.mutex)
            {
                this.abortingTXs.Add(txId, abortingTx);
            }
            
            this.bus.AbortTx(proposerId, new TxAbort(txId));

            lock (abortingTx.mutex)
            {
                abortingTx.hasSent = true;
            }
            
            this.timer.SetTimeout(() =>
            {
                lock (abortingTx.mutex)
                {
                    if (!abortingTx.hasFinished)
                    {
                        abortingTx.hasFinished = true;
                        abortingTx.tcs.SetException(new TxUnknownException(txId));
                    }
                }
            }, timeoutMs);

            var txDetails = await abortingTx.tcs.Task;
            
            var rollbackMsg = new RollbackTxMessage(txId);

            foreach (var shardId in txDetails.shardIds)
            {
                this.bus.RollbackTx(shardId, rollbackMsg.Clone());
            }
        }
        
        public Task<TxStatus> FetchTxStatus(string txId)
        {
            throw new NotImplementedException();
        }
        
        public void OnTxConfirmation(string senderId, TxConfirmationMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.ongoingTXs.ContainsKey(msg.TxID)) return;
                
                var ongoing = this.ongoingTXs[msg.TxID];
                
                lock (ongoing.mutex)
                {
                    if (!ongoing.acceptors.Contains(senderId)) return;

                    ongoing.acks.Add(senderId);
                    ongoing.result = msg.Result;

                    if (!ongoing.HasMajority()) return;

                    ongoing.hasFinished = true;
                    this.ongoingTXs.Remove(ongoing.txId);
                }
                
                ongoing.tcs.SetResult(ongoing.result);
            }
        }

        public void OnTxConflict(string senderId, TxConflictMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.ongoingTXs.ContainsKey(msg.TxID)) return;
                
                var ongoing = this.ongoingTXs[msg.TxID];
                
                lock (ongoing.mutex)
                {
                    if (!ongoing.shards.Contains(senderId)) return;

                    ongoing.hasFinished = true;
                    this.ongoingTXs.Remove(ongoing.txId);
                }
                
                ongoing.tcs.SetException(new TxConflictException(ongoing.txId, new []{ msg.ConflictingTxID }));
            }
        }

        public void OnTxAborted(string proposerId, TxAbortedMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.abortingTXs.ContainsKey(msg.TxID)) return;
                
                var aborting = this.abortingTXs[msg.TxID];
                
                lock (aborting.mutex)
                {
                    if (aborting.proposerId != proposerId) return;

                    aborting.hasFinished = true;
                    this.abortingTXs.Remove(aborting.txId);
                }
                
                aborting.tcs.SetResult(new TxDetails(msg.ShardIDs));
            }
        }

        public void OnTxCommitted(string proposerId, TxCommittedMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.abortingTXs.ContainsKey(msg.TxID)) return;
                
                var aborting = this.abortingTXs[msg.TxID];
                
                lock (aborting.mutex)
                {
                    if (aborting.proposerId != proposerId) return;

                    aborting.hasFinished = true;
                    this.abortingTXs.Remove(aborting.txId);
                }
                
                aborting.tcs.SetException(new AlreadyCommittedException());
            }
        }
    }
}