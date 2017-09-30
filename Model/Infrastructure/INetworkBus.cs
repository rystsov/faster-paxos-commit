using System;
using Model.Services.Acceptor.Messages;
using Model.Services.Client.Messages;
using Model.Services.Proposer.Messages;
using Model.Services.Shard.Messages;

namespace Model.Infrastructure
{
    public interface INetworkBus
    {
        void ExecuteSubTx(string shardId, InitiateTxMessage msg);
        void WaitForExecutionAccepted(string reqId, Func<ExecutionAcceptedMessage, string, WaitStrategy> handler);
        void WaitForExecutionConflicted(string reqId, Func<ExecutionConflictedMessage, string, WaitStrategy> handler);
        
        //public void OnTxAccepted(string acceptorId, TxAcceptedMessage msg)
        
        void MarkSubTxCommitted(string shardId, MarkTxComittedMessage msg);
        void NotifySubTxMarkedCommitted(string clientId, SubTxMarkedComittedMessage msg);
        void WaitForSubTxMarkedCommitted(string reqId, Func<SubTxMarkedComittedMessage, string, WaitStrategy> handler);
        
        void FetchTxStatus(string proposerId, FetchTxStatusMessage msg);
        void WaitForTxStatusFetched(string reqId, Func<TxStatusFetchedMessage, string, WaitStrategy> handler);
        
        void AbortTx(string proposerId, TxAbortMessage msg);
        void WaitForAbortConfirmed(string reqId, Func<AbortConfirmedMessage, string, WaitStrategy> handler);
        void WaitForAbortFailed(string reqId, Func<AbortFailedMessage, string, WaitStrategy> handler);
        
        void CommitSubTx(string acceptorId, TxAcceptedMessage msg);
        void WaitForSubTxAccepted(string reqId, Func<TxAcceptedMessage, string, WaitStrategy> handler);
        
        void PrepareTxArguments(string acceptorId, PrepareTxArgumentsMessage msg);
        void RollbackTx(string shardId, RollbackSubTxMessage msg);
        void NotifyExecutionConflicted(string clientId, ExecutionConflictedMessage msg);
        void RmTx(string proposerId, RmTxMessage msg);
    }
}