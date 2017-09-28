namespace Model.Services.Client.Messages
{
    public class FetchedTxStatusMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public TxStatus Status { get; }

        public FetchedTxStatusMessage(string reqId, string txId, TxStatus status)
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.Status = status;
        }

        public FetchedTxStatusMessage Clone()
        {
            return new FetchedTxStatusMessage(this.ReqID, this.TxID, this.Status);
        }
    }
}