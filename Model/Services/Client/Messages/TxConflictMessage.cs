namespace Model.Services.Client.Messages
{
    public class TxConflictMessage
    {
        public string TxID { get; }
        public string ConflictingTxID { get; }

        public TxConflictMessage(string txId, string conflictingTxId)
        {
            this.TxID = txId;
            this.ConflictingTxID = conflictingTxId;
        }

        public TxConflictMessage Clone()
        {
            return new TxConflictMessage(this.TxID, this.ConflictingTxID);
        }
    }
}