using System;

namespace Model.Services.Client.Exceptions
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