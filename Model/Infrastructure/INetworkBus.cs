using Model.Services.Acceptor.Messages;
using Model.Services.Proposer.Messages;
using Model.Services.Shard.Messages;

namespace Model.Infrastructure
{
    public interface INetworkBus
    {
        void ExecuteSubTx(string shardId, SubTxMessage msg);
        void SendArguments(string acceptorId, TxArgumentsMessage msg);
        void AbortTx(string proposerId, TxAbort msg);
        void RollbackTx(string shardId, RollbackTxMessage msg);
    }
}