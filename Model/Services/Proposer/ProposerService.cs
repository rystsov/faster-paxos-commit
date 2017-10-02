using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Model.Infrastructure;
using Model.Infrastructure.AcceptorStorage;
using Model.Infrastructure.ProposerStorage;
using Model.Services.Acceptor.Messages;
using Model.Services.Client.Messages;
using Model.Services.Proposer.Messages;

namespace Model.Services.Proposer
{
    public class ProposerService
    {
        private class ConflictException : Exception
        {
            public BallotNumber Future { get; }

            public ConflictException(BallotNumber future)
            {
                this.Future = future;
            }
        }
        
        private readonly IServiceLocator locator;
        private readonly INetworkBus bus;
        private readonly ITimer timer;
        private readonly IProposerStorage storage;
        private readonly object mutex = new object();
        
        private BallotNumber ballotNumber;
        
        public ProposerService(IProposerStorage storage, IServiceLocator locator, INetworkBus bus, ITimer timer)
        {
            this.locator = locator;
            this.bus = bus;
            this.timer = timer;
            this.storage = storage;
        }

        public async Task Init()
        {
            this.ballotNumber = await this.storage.LoadBallotNumber();
        }
        
        public async Task AbortTx(string clientId, TxAbortMessage msg, int timeoutMs)
        {
            var acceptors = this.locator.GetAcceptorIDs();
            
            BallotNumber ballot = null;
            lock (this.mutex)
            {
                ballot = this.ballotNumber.Inc();
                this.ballotNumber = ballot;
            }

            TxState state;

            try
            {
                state = await this.FetchState(acceptors, msg.TxID, ballot, timeoutMs);
                if (!state.IsAborted && !state.IsComitted)
                {
                    state.IsAborted = true;
                }
                await this.UpdateState(acceptors, ballot, state, timeoutMs);
            }
            catch (ConflictException e)
            {
                ballot = await this.storage.FastForward(e.Future);
                lock (this.mutex)
                {
                    if (ballot.GreaterThan(this.ballotNumber))
                    {
                        this.ballotNumber = ballot;
                    }
                }
                throw;
            }
            
            var shardIDs = new HashSet<string>();
            if (state.ShardKeyValueUpdate != null)
            {
                shardIDs = new HashSet<string>((IEnumerable<string>)state.ShardKeyValueUpdate.Keys);
            }

            if (state.IsAborted)
            {
                this.bus.NotifyAbortConfirmed(clientId, msg.ReqID, new AbortConfirmedMessage(shardIDs));
            }
            else
            {
                this.bus.NotifyAbortFailed(clientId, msg.ReqID, new AbortFailedMessage(state.ShardKeyValueUpdate));
            }
        }

        private Task<TxState> FetchState(ISet<string> acceptors, string txId, BallotNumber ballot, int timeoutMs)
        {
            var reqId = Guid.NewGuid().ToString();
            var localMutex = new object();
            var isPromised = false;

            foreach (var acceptorId in acceptors)
            {
                this.bus.Promise(acceptorId, reqId, new PromiseMessage(txId, ballot));
            }
            
            var maxState = new TaskCompletionSource<TxState>();
            PromiseAcceptedMessage max = null;
            var acks = new HashSet<string>();

            var handler1 = this.bus.WaitForPromiseAccepted(reqId, (promised, acceptorId) =>
            {
                lock (localMutex)
                {
                    if (!acceptors.Contains(acceptorId)) return WaitStrategy.KeepWaiting;
                    if (max == null)
                    {
                        max = promised;
                    }
                    if (promised.PrevBallot.GreaterThan(max.PrevBallot))
                    {
                        max = promised;
                    }
                    acks.Add(acceptorId);
                    if (acks.Count >= 1 + acceptors.Count / 2)
                    {
                        isPromised = true;
                        maxState.SetResult(max.State);
                        return WaitStrategy.StopWaiting;
                    }
                    return WaitStrategy.KeepWaiting;
                }
            });
            
            var handler2 = this.bus.WaitForPromiseConflicted(reqId, (conflicted, sender) =>
            {
                lock (localMutex)
                {
                    if (isPromised) return WaitStrategy.StopWaiting;
                    isPromised = true;
                    maxState.SetException(new ConflictException(conflicted.Future));
                    return WaitStrategy.StopWaiting;
                }
            });
            
            this.timer.SetTimeout(() =>
            {
                lock (localMutex)
                {
                    if (isPromised) return;
                    maxState.SetException(new TimeoutException());
                    handler1.Dispose();
                    handler2.Dispose();
                }
            }, timeoutMs);

            return maxState.Task; 
        }
        
        private async Task UpdateState(ISet<string> acceptors, BallotNumber ballot, TxState state, int timeoutMs)
        {
            var reqId = Guid.NewGuid().ToString();
            var localMutex = new object();
            var isAccepted = false;
            var acks = new HashSet<string>();
            var accepted = new TaskCompletionSource<bool>();
            
            foreach (var acceptorId in acceptors)
            {
                this.bus.AcceptUpdate(acceptorId, reqId, new AcceptUpdateMessage(ballot, state));
            }
            
            var handler1 = this.bus.WaitForUpdateAccepted(reqId, (_, acceptorId) =>
            {
                lock (localMutex)
                {
                    if (isAccepted) return WaitStrategy.StopWaiting;
                    acks.Add(acceptorId);
                    if (acks.Count >= 1 + acceptors.Count / 2)
                    {
                        isAccepted = true;
                        accepted.SetResult(true);
                        return WaitStrategy.StopWaiting;
                    }
                    return WaitStrategy.KeepWaiting;
                }
            });
            
            this.timer.SetTimeout(() =>
            {
                lock (localMutex)
                {
                    if (isAccepted) return;
                    accepted.SetException(new TimeoutException());
                    handler1.Dispose();
                }
            }, timeoutMs);

            _ = await accepted.Task; 
        }
    }
}