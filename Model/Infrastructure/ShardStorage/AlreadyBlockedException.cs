using System;
using System.Collections.Generic;

namespace Model.Infrastructure.ShardStorage
{
    public class AlreadyBlockedException : Exception
    {
        public Dictionary<string, string> BlockedKeyTxPairs { get; }

        public AlreadyBlockedException()
        {
            this.BlockedKeyTxPairs = new Dictionary<string, string>();
        }
    }
}