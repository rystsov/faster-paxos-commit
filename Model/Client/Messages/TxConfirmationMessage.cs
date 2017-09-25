using System.Collections.Generic;

namespace Model.Client.Messages
{
    public class TxConfirmationMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> Result { get; }

        public TxConfirmationMessage(string txId) : this(txId, new Dictionary<string, string>())
        {
        }
        
        public TxConfirmationMessage(string txId, Dictionary<string, string> result)
        {
            this.TxID = txId;
            this.Result = result;
        }

        public TxConfirmationMessage Clone()
        {
            return new TxConfirmationMessage(
                this.TxID,
                new Dictionary<string, string>(this.Result)
            );
        }
    }
}