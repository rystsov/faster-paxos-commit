using Model.Services.Acceptor.Messages;
using Model.Services.Client.Messages;
using Model.Services.Proposer.Messages;
using Model.Services.Shard.Messages;

namespace Model.Infrastructure
{
    public interface INetworkBus
    {
        void ExecuteSubTx(string shardId, InitiateTxMessage msg);
        void PrepareTxArguments(string acceptorId, PrepareTxArgumentsMessage msg);
        void AbortTx(string proposerId, TxAbortMessage msg);
        void RollbackTx(string shardId, RollbackSubTxMessage msg);
        void FetchTxStatus(string proposerId, FetchTxStatusMessage msg);
        void ConfirmSubTx(string acceptorId, TxAcceptedMessage msg);
        void MarkSubTxCommitted(string shardId, MarkTxComittedMessage msg);
        void AlreadyBlockedError(string clientId, ExecutionConflictedMessage msg);
        void RmTx(string proposerId, RmTxMessage msg);
        void NotifySubTxMarkedCommitted(string clientId, SubTxMarkedComittedMessage msg);
    }
}