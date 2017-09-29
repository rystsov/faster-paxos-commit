using System.Collections.Generic;

namespace Model.Services.Acceptor.Messages
{
    public class SubTxConfirmationMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> KeyValueUpdate { get; }

        public SubTxConfirmationMessage(string txId, Dictionary<string, string> keyValueUpdate)
        {
            this.TxID = txId;
            this.KeyValueUpdate = keyValueUpdate;
        }

        public SubTxConfirmationMessage Clone()
        {
            return new SubTxConfirmationMessage(
                txId: this.TxID, 
                keyValueUpdate: new Dictionary<string, string>(this.KeyValueUpdate)
            );
        }
    }
}