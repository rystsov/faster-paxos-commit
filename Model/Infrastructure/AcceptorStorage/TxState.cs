using System.Collections.Generic;

namespace Model.Infrastructure.AcceptorStorage
{
    public class TxState
    {
        public string TxID { get; set; }
        public Dictionary<string, Dictionary<string, string>> ShardKeyValueUpdate { get; set; }
        public bool IsAborted { get; set; }
        public bool IsComitted { get; set; }
        
        public TxState(string txId,
            Dictionary<string, Dictionary<string, string>> shardKeyValueUpdate, bool isAborted, bool isComitted)
        {
            this.TxID = txId;
            this.ShardKeyValueUpdate = shardKeyValueUpdate;
            this.IsAborted = isAborted;
            this.IsComitted = isComitted;
        }

        public TxState Clone()
        {
            var clone = new TxState(this.TxID, new Dictionary<string, Dictionary<string, string>>(), this.IsAborted, this.IsComitted);
            foreach (var shardId in this.ShardKeyValueUpdate.Keys)
            {
                clone.ShardKeyValueUpdate.Add(shardId, new Dictionary<string, string>(this.ShardKeyValueUpdate[shardId]));
            }
            return clone;
        }
    }
}