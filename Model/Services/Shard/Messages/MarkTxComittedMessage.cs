using System.Collections.Generic;

namespace Model.Services.Shard.Messages
{
    public class MarkTxComittedMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public Dictionary<string, string> KeyValueUpdate { get; }

        public MarkTxComittedMessage(string reqId, string txId, Dictionary<string, string> keyValueUpdate)
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.KeyValueUpdate = keyValueUpdate;
        }
    }
}