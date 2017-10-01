using System.Collections.Generic;
using Model.Services.Acceptor.Messages;

namespace Model.Services.Shard.Messages
{
    public class SubTxAcceptedMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> KeyValueUpdate { get; }

        public SubTxAcceptedMessage(string txId, Dictionary<string, string> keyValueUpdate)
        {
            this.TxID = txId;
            this.KeyValueUpdate = keyValueUpdate;
        }

        public SubTxAcceptedMessage Clone()
        {
            return new SubTxAcceptedMessage(
                txId: this.TxID, 
                keyValueUpdate: new Dictionary<string, string>(this.KeyValueUpdate)
            );
        }
    }
}