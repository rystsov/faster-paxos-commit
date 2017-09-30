namespace Model.Services.Client.Messages
{
    public class TxStatusFetchedMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public TxStatus Status { get; }

        public TxStatusFetchedMessage(string reqId, string txId, TxStatus status)
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.Status = status;
        }

        public TxStatusFetchedMessage Clone()
        {
            return new TxStatusFetchedMessage(this.ReqID, this.TxID, this.Status);
        }
    }
}