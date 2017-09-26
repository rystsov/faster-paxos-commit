using Model.Services.Acceptor.Messages;
using Model.Services.Shard.Messages;

namespace Model.Infrastructure
{
    public interface INetworkBus
    {
        void ExecuteSubTx(string shardId, SubTxMessage msg);
        void SendArguments(string acceptorId, TxArgumentsMessage msg);
    }
}