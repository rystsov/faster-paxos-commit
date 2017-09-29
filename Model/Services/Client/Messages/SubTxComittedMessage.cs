namespace Model.Services.Client.Messages
{
    public class SubTxComittedMessage
    {
        public string ReqID { get; }

        public SubTxComittedMessage(string reqId)
        {
            this.ReqID = reqId;
        }

        public TxComittedMessage Clone()
        {
            return new TxComittedMessage(
                this.ReqID
            );
        }
    }
}