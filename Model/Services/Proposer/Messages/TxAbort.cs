using System.Collections.Generic;

namespace Model.Services.Proposer.Messages
{
    public class TxAbort
    {
        public string TxID { get; }
        
        public TxAbort(string txId)
        {
            this.TxID = txId;
        }

        public TxAbort Clone()
        {
            return new TxAbort(this.TxID);
        }
    }
}