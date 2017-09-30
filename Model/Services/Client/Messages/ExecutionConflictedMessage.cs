using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class ExecutionConflictedMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> KeyBlockedByTX { get; }

        public ExecutionConflictedMessage(string txId, Dictionary<string, string> keyBlockedByTx)
        {
            this.TxID = txId;
            this.KeyBlockedByTX = keyBlockedByTx;
        }

        public ExecutionConflictedMessage Clone()
        {
            return new ExecutionConflictedMessage(
                txId: this.TxID, 
                keyBlockedByTx: new Dictionary<string, string>(this.KeyBlockedByTX)
            );
        }
    }
}