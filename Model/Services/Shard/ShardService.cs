using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Model.Infrastructure;
using Model.Infrastructure.ShardStorage;
using Model.Services.Client.Messages;
using Model.Services.Shard.Messages;
using SubTxConfirmationMessage = Model.Services.Acceptor.Messages.SubTxConfirmationMessage;

namespace Model.Services.Shard
{
    public class ShardImpl
    {
        private class AbortException : Exception {}
        
        private class InProgressSubTx
        {
            public readonly TaskCompletionSource<Dictionary<string, string>> tcs = new TaskCompletionSource<Dictionary<string, string>>();
            public readonly object mutex = new object();
            public readonly string txId;
            public Dictionary<string, string> keyValueUpdate;
            public readonly ISet<string> acks = new HashSet<string>();
            public readonly ISet<string> nacks = new HashSet<string>();
            public readonly ISet<string> acceptors;
            public bool hasSent;
            public bool hasFinished;

            public InProgressSubTx(ISet<string> acceptors, string txId)
            {
                this.txId = txId;
                this.acceptors = acceptors;
                this.hasSent = false;
                this.hasFinished = false;
            }

            public bool HasMajority()
            {
                return this.acks.Count >= 1 + acceptors.Count / 2;
            }
        }
        
        private readonly IShardStorage storage;
        private readonly INetworkBus bus;
        private readonly IServiceLocator locator;
        private readonly ITimer timer;
        private readonly object mutex = new object();
        private readonly Dictionary<string, InProgressSubTx> ongoingTXs = new Dictionary<string, InProgressSubTx>();

        public ShardImpl(IShardStorage storage, INetworkBus bus, IServiceLocator locator, ITimer timer)
        {
            this.storage = storage;
            this.bus = bus;
            this.locator = locator;
            this.timer = timer;
        }

        public async Task OnExecuteSubTx(string clientId, ExecuteSubTxMessage msg, int timeoutMs)
        {
            Dictionary<string, string> values;
            try
            {
                values = await this.storage.ReadAndBlock(msg.TxID, msg.TxName, msg.ShardIDs, msg.Keys);
            }
            catch (AlreadyBlockedException ex)
            {
                this.bus.AlreadyBlockedError(clientId, new ExecutionConflictedMessage(msg.TxID, ex.BlockedKeyTxPairs));
                return;
            }
            
            var acceptors = this.locator.GetAcceptorIDs();
                
            var ongoing = new InProgressSubTx(acceptors, msg.TxID);

            lock (this.mutex)
            {
                this.ongoingTXs.Add(msg.TxID, ongoing);
            }
                
            var confirmation = new SubTxConfirmationMessage(msg.TxID, values);
            foreach (var acceptorID in acceptors)
            {
                this.bus.ConfirmSubTx(acceptorID, confirmation.Clone());
            }

            lock (ongoing.mutex)
            {
                ongoing.hasSent = true;
            }
                
            this.timer.SetTimeout(() =>
            {
                lock (this.mutex)
                {
                    lock (ongoing.mutex)
                    {
                        if (!ongoing.hasFinished)
                        {
                            ongoing.hasFinished = true;
                            if (this.ongoingTXs.ContainsKey(ongoing.txId))
                            {
                                this.ongoingTXs.Remove(ongoing.txId);
                            }
                            ongoing.tcs.SetException(new AbortException());
                        }
                    }
                }
            }, timeoutMs);
                
            lock (ongoing.mutex)
            {
                ongoing.hasSent = true;
            }

            Dictionary<string, string> update;

            try
            {
                update = await ongoing.tcs.Task;
            }
            catch (AbortException)
            {
                return;
            }
            
            await this.storage.UpdateAndUnblock(msg.TxID, update);
        }

        public async Task OnRollbackSubTx(string _, RollbackSubTxMessage msg)
        {
            await this.storage.Unblock(msg.TxID);
        }

        public void OnSubTxConfirmation(string acceptorId, SubTxConfirmationMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.ongoingTXs.ContainsKey(msg.TxID)) return;
                
                var ongoing = this.ongoingTXs[msg.TxID];
                
                lock (ongoing.mutex)
                {
                    if (!ongoing.acceptors.Contains(acceptorId)) return;

                    ongoing.acks.Add(acceptorId);
                    ongoing.keyValueUpdate = msg.KeyValueUpdate;

                    if (!ongoing.HasMajority()) return;

                    ongoing.hasFinished = true;
                    this.ongoingTXs.Remove(ongoing.txId);
                }
                
                ongoing.tcs.SetResult(ongoing.keyValueUpdate);
            }
        }

        public async Task OnMarkSubTxComitted(string clientId, MarkSubTxCommittedMessage msg)
        {
            await this.storage.UpdateAndUnblock(msg.TxID, msg.KeyValueUpdate);
            
            this.bus.NotifySubTxMarkedCommitted(clientId, new SubTxMarkedComittedMessage(msg.ReqID));
        }
    }
}