namespace Model.Services.Client.Messages
{
    public class TxCommittedMessage
    {
        public string ReqID { get; }
        public string TxID { get; }

        public TxCommittedMessage(string reqId, string txId)
        {
            this.ReqID = reqId;
            this.TxID = txId;
        }

        public TxCommittedMessage Clone()
        {
            return new TxCommittedMessage(this.ReqID, this.TxID);
        }
    }
}