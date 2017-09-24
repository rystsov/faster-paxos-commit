namespace Model.Client
{
    public enum TxStatus
    {
        /// <summary>
        /// Tx is either garbage collected after being committed or aborted, or never existed
        /// </summary>
        GGed,
        /// <summary>
        /// Tx is committed, eventially will be GGed
        /// </summary>
        Committed,
        /// <summary>
        /// Tx is aborted, eventially will be GGed
        /// </summary>
        Aborted,
        /// <summary>
        /// Tx is on going, eventially will be committed or aborted
        /// </summary>
        OnGoing
    }
}