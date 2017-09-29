using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class TxAlreadyCommittedMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public Dictionary<string, Dictionary<string, string>> KeyValueUpdateByShard { get; }

        public TxAlreadyCommittedMessage(
            string reqId, 
            string txId,
            Dictionary<string, Dictionary<string, string>> keyValueUpdateByShard
        )
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.KeyValueUpdateByShard = keyValueUpdateByShard;
        }

        public TxAlreadyCommittedMessage Clone()
        {
            var keyValueUpdateByShard = new Dictionary<string, Dictionary<string, string>>();
            foreach (var key in this.KeyValueUpdateByShard.Keys)
            {
                keyValueUpdateByShard.Add(key, new Dictionary<string, string>(this.KeyValueUpdateByShard[key]));
            }
            
            return new TxAlreadyCommittedMessage(
                this.ReqID, 
                this.TxID,
                keyValueUpdateByShard
            );
        }
    }
}