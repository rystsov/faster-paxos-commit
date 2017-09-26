namespace Model.Services.Client.Messages
{
    public class TxCommittedMessage
    {
        public string TxID { get; }

        public TxCommittedMessage(string txId)
        {
            this.TxID = txId;
        }

        public TxCommittedMessage Clone()
        {
            return new TxCommittedMessage(this.TxID);
        }
    }
}