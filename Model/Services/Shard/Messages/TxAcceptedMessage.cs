using System.Collections.Generic;

namespace Model.Services.Shard.Messages
{
    public class TxAcceptedMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> KeyValueUpdate { get; }

        public TxAcceptedMessage(string txId, Dictionary<string, string> keyValueUpdate)
        {
            this.TxID = txId;
            this.KeyValueUpdate = keyValueUpdate;
        }

        public TxAcceptedMessage Clone()
        {
            return new TxAcceptedMessage(
                txId: this.TxID, 
                keyValueUpdate: new Dictionary<string, string>(this.KeyValueUpdate)
            );
        }
    }
}