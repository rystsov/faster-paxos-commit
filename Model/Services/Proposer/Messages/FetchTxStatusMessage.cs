using Model.Services.Client;

namespace Model.Services.Proposer.Messages
{
    public class FetchTxStatusMessage
    {
        public string ReqID { get; }
        public string TxID { get; }

        public FetchTxStatusMessage(string reqId, string txId)
        {
            this.ReqID = reqId;
            this.TxID = txId;
        }

        public FetchTxStatusMessage Clone()
        {
            return new FetchTxStatusMessage(this.ReqID, this.TxID);
        }
    }
}