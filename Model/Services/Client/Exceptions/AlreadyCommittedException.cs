using System;
using System.Collections.Generic;

namespace Model.Services.Client.Exceptions
{
    public class AlreadyCommittedException : Exception
    {
        public string TxID { get; }
        public Dictionary<string, Dictionary<string, string>> KeyValueUpdateByShard { get; }

        public AlreadyCommittedException(
            string txId,
            Dictionary<string, Dictionary<string, string>> keyValueUpdateByShard
        )
        {
            this.TxID = txId;
            this.KeyValueUpdateByShard = keyValueUpdateByShard;
        }
    }
}