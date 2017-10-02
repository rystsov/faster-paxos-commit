using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Model.Infrastructure;
using Model.Infrastructure.AcceptorStorage;
using Model.Services.Acceptor.Messages;
using Model.Services.Client.Messages;
using Model.Services.Proposer.Messages;
using Model.Services.Shard.Messages;

namespace Model.Services.Acceptor
{
    public class AcceptorService
    {
        private class Tx : ITx
        {
            public string ClientID { get; set; }
            public string TxID { get; }
            public string TxName { get; set; }
            public Dictionary<string, string> Args { get; set; }
            public ISet<string> ShardIDs { get; set; }
            public Dictionary<string, Dictionary<string, string>> ShardKeyValue { get; }

            public Tx(string txId)
            {
                this.TxID = txId;
                this.ShardKeyValue = new Dictionary<string, Dictionary<string, string>>();
            }
        }

        private class OngoingTx
        {
            public readonly object localMutex = new object();
            public Tx tx;
            public bool hasFinished;
            public bool hasArgs;
            
            public bool IsReady
            {
                get { return this.hasArgs && this.tx.ShardKeyValue.Keys.All(this.tx.ShardIDs.Contains); }
            }

            public OngoingTx(string txId)
            {
                this.tx = new Tx(txId);
                this.hasFinished = false;
                this.hasArgs = false;
            }
        }

        private readonly INetworkBus bus;
        private readonly ITimer timer;
        private readonly ITxExecutor executor;
        private readonly IAcceptorStorage storage;
        private readonly object mutex = new object();
        private readonly Dictionary<string, OngoingTx> txs = new Dictionary<string, OngoingTx>();

        public AcceptorService(INetworkBus bus, ITimer timer, ITxExecutor executor, IAcceptorStorage storage)
        {
            this.bus = bus;
            this.timer = timer;
            this.executor = executor;
            this.storage = storage;
        }
        
        public void CommitTx(string shardId, CommitTxMessage msg, int timeoutMs)
        {
            var ongoingTx = this.EnsureTxIsOngoing(msg.TxID, timeoutMs);

            lock (this.mutex)
            {
                lock (ongoingTx.localMutex)
                {
                    if (ongoingTx.hasFinished) return;
                    ongoingTx.tx.ShardKeyValue[msg.ShardID] = msg.KeyValueUpdate;
                    if (ongoingTx.IsReady)
                    {
                        ongoingTx.hasFinished = true;
                        this.txs.Remove(ongoingTx.tx.TxID);
                        _ = Execute(ongoingTx.tx);
                    }
                }
            }
        }

        public void PrepareTxArguments(string senderId, PrepareTxArgumentsMessage msg, int timeoutMs)
        {
            var ongoingTx = this.EnsureTxIsOngoing(msg.TxID, timeoutMs);

            lock (this.mutex)
            {
                lock (ongoingTx.localMutex)
                {
                    if (ongoingTx.hasFinished) return;
                    if (ongoingTx.hasArgs) return;

                    ongoingTx.hasArgs = true;
                    ongoingTx.tx.Args = msg.Args;
                    ongoingTx.tx.ShardIDs = msg.ShardIDs;
                    ongoingTx.tx.TxName = msg.TxName;
                    ongoingTx.tx.ClientID = msg.ClientID;
                    
                    if (ongoingTx.IsReady)
                    {
                        ongoingTx.hasFinished = true;
                        this.txs.Remove(ongoingTx.tx.TxID);
                        _ = Execute(ongoingTx.tx);
                    }
                }
            }
        }

        public async Task Promise(string proposerId, PromiseMessage msg)
        {
            try
            {
                var status = await this.storage.Promise(msg.TxID, msg.Ballot);
                this.bus.NotifyPromiseAccepted(proposerId, msg.ReqID, new PromiseAcceptedMessage(
                    status.AcceptedBallot,
                    status.State
                ));
            }
            catch (ConflictException e)
            {
                this.bus.NotifyPromiseConflicted(proposerId, msg.ReqID, new PromiseConflictedMessage(
                    e.Future
                ));
            }
        }

        public async Task Accept(string proposerId, AcceptUpdateMessage msg)
        {
            await this.storage.Accept(msg.TxID, msg.Ballot, msg.State);
            this.bus.NotifyUpdateAccepted(proposerId, msg.ReqID);
        }

        public async Task RmTx(string clientId, RmTxMessage msg)
        {
            await this.storage.RmTx(msg.TxID);
        }

        private async Task Execute(Tx tx)
        {
            try
            {
                var result = this.executor.Execute(tx);
            
                await this.storage.Commit(tx.TxID, result.ShardKeyValueUpdates);
            
                foreach (var shardId in result.ShardKeyValueUpdates.Keys)
                {
                    this.bus.NotifySubTxAccepted(
                        shardId, 
                        tx.TxID,
                        new SubTxAcceptedMessage(tx.TxID, result.ShardKeyValueUpdates[shardId])
                    );
                }
            
                this.bus.NotifyExecutionAccepted(
                    tx.ClientID, 
                    tx.TxID, 
                    new ExecutionAcceptedMessage(tx.TxID, result.Result)
                );
            }
            catch (Exception)
            {
                // TODO: propaganate a error to a client (like unknown tx, tx execution error)
            }
        }

        private OngoingTx EnsureTxIsOngoing(string txId, int timeoutMs)
        {
            lock (this.mutex)
            {
                if (!this.txs.ContainsKey(txId))
                {
                    var ongoingTx = new OngoingTx(txId);
                    this.txs.Add(txId, ongoingTx);
                    this.timer.SetTimeout(() => 
                    {
                        lock (this.mutex)
                        {
                            lock (ongoingTx.localMutex)
                            {
                                ongoingTx.hasFinished = true;
                                this.txs.Remove(txId);
                            }
                        }
                    }, timeoutMs);
                }
                return this.txs[txId];
            }
        }
    }
}