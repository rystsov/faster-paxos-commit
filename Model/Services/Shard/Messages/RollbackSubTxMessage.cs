namespace Model.Services.Shard.Messages
{
    public class RollbackSubTxMessage
    {
        public string TxID { get; }

        public RollbackSubTxMessage(string txId)
        {
            this.TxID = txId;
        }

        public RollbackSubTxMessage Clone()
        {
            return new RollbackSubTxMessage(this.TxID);
        }
    }
}