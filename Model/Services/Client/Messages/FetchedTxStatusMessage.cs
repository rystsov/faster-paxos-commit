namespace Model.Services.Client.Messages
{
    public class FetchedTxStatusMessage
    {
        public string TxID { get; }
        public TxStatus Status { get; }

        public FetchedTxStatusMessage(string txId, TxStatus status)
        {
            this.TxID = txId;
            this.Status = status;
        }

        public FetchedTxStatusMessage Clone()
        {
            return new FetchedTxStatusMessage(this.TxID, this.Status);
        }
    }
}