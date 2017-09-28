using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class TxAbortedMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public IEnumerable<string> ShardIDs { get;  }

        public TxAbortedMessage(string reqId, string txId, IEnumerable<string> shardIDs)
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.ShardIDs = shardIDs;
        }

        public TxAbortedMessage Clone()
        {
            return new TxAbortedMessage(this.ReqID, this.TxID, this.ShardIDs);
        }
    }
}