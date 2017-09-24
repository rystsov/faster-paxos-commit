using System;
using System.Collections.Generic;

namespace Model.Client
{
    public class TxConflictException : Exception
    {
        public string TxID { get; }
        public IEnumerable<string> ConflictingTXs { get; }

        public TxConflictException(string txId, IEnumerable<string> conflictingTXs)
        {
            TxID = txId;
            ConflictingTXs = conflictingTXs;
        }
    }
}