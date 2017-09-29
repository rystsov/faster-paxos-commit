using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class TxComittedMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> Result { get; }

        public TxComittedMessage(string txId) : this(txId, new Dictionary<string, string>())
        {
        }
        
        public TxComittedMessage(string txId, Dictionary<string, string> result)
        {
            this.TxID = txId;
            this.Result = result;
        }

        public TxComittedMessage Clone()
        {
            return new TxComittedMessage(
                this.TxID,
                new Dictionary<string, string>(this.Result)
            );
        }
    }
}