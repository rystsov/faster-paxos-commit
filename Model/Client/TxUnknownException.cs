using System;

namespace Model.Client
{
    public class TxUnknownException : Exception
    {
        public string TxID { get; }

        public TxUnknownException(string txId)
        {
            this.TxID = txId;
        }
    }
}