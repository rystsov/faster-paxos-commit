using System;
using System.Collections.Generic;

namespace Model.Services.Client.Exceptions
{
    public class TxConflictException : Exception
    {
        public string TxID { get; }
        public Dictionary<string, string> KeyBlockedByTX { get; }

        public TxConflictException(string txId, Dictionary<string, string> keyBlockedByTx)
        {
            TxID = txId;
            KeyBlockedByTX = keyBlockedByTx;
        }
    }
}