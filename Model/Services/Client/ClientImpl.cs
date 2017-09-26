﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using Model.Infrastructure;
using Model.Services.Acceptor.Messages;
using Model.Services.Client.Exceptions;
using Model.Services.Client.Messages;
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
            public readonly Stopwatch stopwatch;
            public bool hasStarted;
            public bool hasFinished;

            public InProgressTx(ISet<string> shards, ISet<string> acceptors, string txId)
            {
                this.txId = txId;
                this.acceptors = acceptors;
                this.shards = shards;
                this.stopwatch = new Stopwatch();
                this.stopwatch.Start();
                this.hasStarted = false;
            }

            public bool HasMajority()
            {
                return this.acks.Count >= 1 + acceptors.Count / 2;
            }
        }
        
        private readonly IServiceLocator locator;
        private readonly INetworkBus bus;
        private readonly ITimer timer;
        private readonly Dictionary<string, InProgressTx> ongoing = new Dictionary<string, InProgressTx>();
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
                this.ongoing.Add(txId, inProgressTx);
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
                inProgressTx.hasStarted = true;
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

        public Task<TxStatus> FetchTxStatus(string txId)
        {
            throw new NotImplementedException();
        }

        public Task AbortTx(string txId)
        {
            throw new NotImplementedException();
        }

        public void OnTxConfirmation(string senderId, TxConfirmationMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.ongoing.ContainsKey(msg.TxID)) return;
                
                var ongoing = this.ongoing[msg.TxID];
                
                lock (ongoing.mutex)
                {
                    if (!ongoing.acceptors.Contains(senderId)) return;

                    ongoing.acks.Add(senderId);
                    ongoing.result = msg.Result;

                    if (!ongoing.HasMajority()) return;

                    ongoing.hasFinished = true;
                    this.ongoing.Remove(ongoing.txId);
                }
                
                ongoing.tcs.SetResult(ongoing.result);
            }
        }

        public void OnTxConflict(string senderId, TxConflictMessage msg)
        {
            lock (this.mutex)
            {
                if (!this.ongoing.ContainsKey(msg.TxID)) return;
                
                var ongoing = this.ongoing[msg.TxID];
                
                lock (ongoing.mutex)
                {
                    if (!ongoing.shards.Contains(senderId)) return;

                    ongoing.hasFinished = true;
                    this.ongoing.Remove(ongoing.txId);
                }
                
                ongoing.tcs.SetException(new TxConflictException(ongoing.txId, new []{ msg.ConflictingTxID }));
            }
        }
    }
}