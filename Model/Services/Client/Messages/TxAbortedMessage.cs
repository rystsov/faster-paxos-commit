using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class TxAbortedMessage
    {
        public string TxID { get; }
        public IEnumerable<string> ShardIDs { get;  }

        public TxAbortedMessage(string txId, IEnumerable<string> shardIDs)
        {
            this.TxID = txId;
            this.ShardIDs = shardIDs;
        }

        public TxAbortedMessage Clone()
        {
            return new TxAbortedMessage(this.TxID, this.ShardIDs);
        }
    }
}