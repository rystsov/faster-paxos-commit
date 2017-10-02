namespace Model.Services.Acceptor.Messages
{
    public class RmTxMessage
    {
        public string TxID { get; }

        public RmTxMessage(string txId)
        {
            this.TxID = txId;
        }
    }
}