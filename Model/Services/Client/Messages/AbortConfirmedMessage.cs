using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class AbortConfirmedMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public IEnumerable<string> ShardIDs { get;  }

        public AbortConfirmedMessage(string reqId, string txId, IEnumerable<string> shardIDs)
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.ShardIDs = shardIDs;
        }

        public AbortConfirmedMessage Clone()
        {
            return new AbortConfirmedMessage(this.ReqID, this.TxID, this.ShardIDs);
        }
    }
}