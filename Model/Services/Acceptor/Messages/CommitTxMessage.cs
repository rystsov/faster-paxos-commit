using System.Collections.Generic;

namespace Model.Services.Acceptor.Messages
{
    public class CommitTxMessage
    {
        public string ShardID { get; }
        public string TxID { get; }
        public Dictionary<string, string> KeyValueUpdate { get; }

        public CommitTxMessage(string shardId, string txId, Dictionary<string, string> keyValueUpdate)
        {
            this.ShardID = shardId;
            this.TxID = txId;
            this.KeyValueUpdate = keyValueUpdate;
        }

        public CommitTxMessage Clone()
        {
            return new CommitTxMessage(
                shardId: this.ShardID,
                txId: this.TxID, 
                keyValueUpdate: new Dictionary<string, string>(this.KeyValueUpdate)
            );
        }
    }
}