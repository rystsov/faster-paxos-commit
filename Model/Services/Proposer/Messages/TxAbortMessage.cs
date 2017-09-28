using System.Collections.Generic;

namespace Model.Services.Proposer.Messages
{
    public class TxAbortMessage
    {
        public string TxID { get; }
        
        public TxAbortMessage(string txId)
        {
            this.TxID = txId;
        }

        public TxAbortMessage Clone()
        {
            return new TxAbortMessage(this.TxID);
        }
    }
}