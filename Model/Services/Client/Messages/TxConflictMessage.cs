using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class TxConflictMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> KeyBlockedByTX { get; }

        public TxConflictMessage(string txId, Dictionary<string, string> keyBlockedByTx)
        {
            this.TxID = txId;
            this.KeyBlockedByTX = keyBlockedByTx;
        }

        public TxConflictMessage Clone()
        {
            return new TxConflictMessage(
                txId: this.TxID, 
                keyBlockedByTx: new Dictionary<string, string>(this.KeyBlockedByTX)
            );
        }
    }
}