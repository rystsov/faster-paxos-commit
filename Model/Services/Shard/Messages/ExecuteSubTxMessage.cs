using System.Collections.Generic;

namespace Model.Services.Shard.Messages
{
    public class ExecuteSubTxMessage
    {
        public ExecuteSubTxMessage(string txId, string txName, ISet<string> keys, ISet<string> shardIDs)
        {
            this.TxID = txId;
            this.TxName = txName;
            this.Keys = keys;
            this.ShardIDs = shardIDs;
        }

        public string TxID { get; }
        public string TxName { get; }
        public ISet<string> Keys { get; }
        public ISet<string> ShardIDs { get; }

        public ExecuteSubTxMessage Clone()
        {
            return new ExecuteSubTxMessage(
                txId: this.TxID,
                txName: this.TxName,
                keys: new HashSet<string>(this.Keys), 
                shardIDs: new HashSet<string>(this.ShardIDs)
            );
        }
    }
}