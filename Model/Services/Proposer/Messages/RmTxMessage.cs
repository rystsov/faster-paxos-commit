namespace Model.Services.Proposer.Messages
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