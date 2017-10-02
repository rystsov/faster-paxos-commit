using System;
using Model.Services.Acceptor.Messages;
using Model.Services.Client.Messages;
using Model.Services.Proposer.Messages;
using Model.Services.Shard.Messages;

namespace Model.Infrastructure
{
    public interface INetworkBus
    {
        string SelfID { get; }

        void ExecuteSubTx(string shardId, InitiateTxMessage msg);
        IHandler WaitForExecutionAccepted(string reqId, Func<ExecutionAcceptedMessage, string, WaitStrategy> handler);
        IHandler WaitForExecutionConflicted(string reqId, Func<ExecutionConflictedMessage, string, WaitStrategy> handler);
        void NotifyExecutionAccepted(string clientId, string reqId, ExecutionAcceptedMessage msg);
        
        void MarkSubTxCommitted(string shardId, MarkTxComittedMessage msg);
        void NotifySubTxMarkedCommitted(string clientId, SubTxMarkedComittedMessage msg);
        IHandler WaitForSubTxMarkedCommitted(string reqId, Func<SubTxMarkedComittedMessage, string, WaitStrategy> handler);
        
        void FetchTxStatus(string proposerId, FetchTxStatusMessage msg);
        IHandler WaitForTxStatusFetched(string reqId, Func<TxStatusFetchedMessage, string, WaitStrategy> handler);
        
        void AbortTx(string proposerId, TxAbortMessage msg);
        IHandler WaitForAbortConfirmed(string reqId, Func<AbortConfirmedMessage, string, WaitStrategy> handler);
        IHandler WaitForAbortFailed(string reqId, Func<AbortFailedMessage, string, WaitStrategy> handler);
        
        void CommitTx(string acceptorId, CommitTxMessage msg);
        IHandler WaitForSubTxAccepted(string reqId, Func<SubTxAcceptedMessage, string, WaitStrategy> handler);
        void NotifySubTxAccepted(string shardId, string reqId, SubTxAcceptedMessage msg);
        
        void PrepareTxArguments(string acceptorId, PrepareTxArgumentsMessage msg);
        
        void RollbackTx(string shardId, RollbackSubTxMessage msg);
        void NotifyExecutionConflicted(string clientId, ExecutionConflictedMessage msg);
        void RmTx(string proposerId, RmTxMessage msg);
        
        void Propose(string acceptorId, string reqId, ProposeMessage proposeMessage);
    }
}