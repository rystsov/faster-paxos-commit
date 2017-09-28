using Model.Services.Client;

namespace Model.Services.Proposer.Messages
{
    public class FetchTxStatusMessage
    {
        public string TxID { get; }

        public FetchTxStatusMessage(string txId)
        {
            this.TxID = txId;
        }

        public FetchTxStatusMessage Clone()
        {
            return new FetchTxStatusMessage(this.TxID);
        }
    }
}