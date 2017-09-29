﻿using System.Collections.Generic;

namespace Model.Services.Acceptor.Messages
{
    public class TxArgumentsMessage
    {
        public TxArgumentsMessage(string txId, string txName, Dictionary<string, string> args, ISet<string> shardIDs)
        {
            this.TxID = txId;
            this.TxName = txName;
            this.Args = args;
            this.ShardIDs = shardIDs;
        }
        
        public string TxID { get; }
        public string TxName { get; }
        public Dictionary<string, string> Args { get; }
        public ISet<string> ShardIDs { get; }

        public TxArgumentsMessage Clone()
        {
            return new TxArgumentsMessage(
                txId: this.TxID,
                txName: this.TxName,
                args: new Dictionary<string, string>(this.Args),
                shardIDs: new HashSet<string>(this.ShardIDs)
            );
        }
    }
}