using System.Collections.Generic;

namespace Model.Services.Shard.Messages
{
    public class CommitSubTxMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public Dictionary<string, string> KeyValueUpdate { get; }

        public CommitSubTxMessage(string reqId, string txId, Dictionary<string, string> keyValueUpdate)
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.KeyValueUpdate = keyValueUpdate;
        }
    }
}