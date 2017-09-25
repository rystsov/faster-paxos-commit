using System.Collections.Generic;

namespace Model.Shard.Messages
{
    public class SubTxMessage
    {
        public SubTxMessage(string txId, string txName) : this(txId, txName, new List<string>()) { }
        
        private SubTxMessage(string txId, string txName, List<string> keys)
        {
            this.TxID = txId;
            this.TxName = txName;
            this.Keys = keys;
        }

        public string TxID { get; }
        public string TxName { get; }
        public List<string> Keys { get; }

        public SubTxMessage Clone()
        {
            return new SubTxMessage(
                txId: this.TxID,
                txName: this.TxName,
                keys: new List<string>(this.Keys)
            );
        }
    }
}