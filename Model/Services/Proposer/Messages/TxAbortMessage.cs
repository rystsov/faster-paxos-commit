using System.Collections.Generic;

namespace Model.Services.Proposer.Messages
{
    public class TxAbortMessage
    {
        public string TxID { get; }
        public string ReqID { get; }

        public TxAbortMessage(string reqId, string txId)
        {
            this.TxID = txId;
            this.ReqID = reqId;
        }

        public TxAbortMessage Clone()
        {
            return new TxAbortMessage(this.ReqID, this.TxID);
        }
    }
}