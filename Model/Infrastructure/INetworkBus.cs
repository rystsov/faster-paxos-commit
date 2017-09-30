using Model.Services.Acceptor.Messages;
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
        void MarkSubTxCommitted(string shardId, MarkSubTxCommittedMessage msg);
        void AlreadyBlockedError(string clientId, ExecutionConflictedMessage msg);
        void RmTx(string proposerId, RmTxMessage msg);
        void NotifySubTxMarkedCommitted(string clientId, SubTxMarkedComittedMessage msg);
    }
}