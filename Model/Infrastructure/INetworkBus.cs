﻿using Model.Services.Acceptor.Messages;
using Model.Services.Client.Messages;
using Model.Services.Proposer.Messages;
using Model.Services.Shard.Messages;
using SubTxConfirmationMessage = Model.Services.Acceptor.Messages.SubTxConfirmationMessage;

namespace Model.Infrastructure
{
    public interface INetworkBus
    {
        void ExecuteSubTx(string shardId, ExecuteSubTxMessage msg);
        void SendArguments(string acceptorId, TxArgumentsMessage msg);
        void AbortTx(string proposerId, TxAbortMessage msg);
        void RollbackTx(string shardId, RollbackSubTxMessage msg);
        void FetchTxStatus(string proposerId, FetchTxStatusMessage msg);
        void ConfirmSubTx(string acceptorId, SubTxConfirmationMessage msg);
        void CommitSubTx(string shardId, CommitSubTxMessage msg);
        void AlreadyBlockedError(string clientId, TxConflictMessage msg);
        void RmTx(string proposerId, RmTxMessage msg);
        void SubTxCommitted(string clientId, SubTxComittedMessage msg);
    }
}