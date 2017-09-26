namespace Model.Services.Shard.Messages
{
    public class RollbackTxMessage
    {
        public string TxID { get; }

        public RollbackTxMessage(string txId)
        {
            this.TxID = txId;
        }

        public RollbackTxMessage Clone()
        {
            return new RollbackTxMessage(this.TxID);
        }
    }
}