using System.Collections.Generic;
using Model.Shard.Messages;

namespace Model.Acceptor.Messages
{
    public class TxArgumentsMessage
    {
        public TxArgumentsMessage(string txId, Dictionary<string, string> args)
        {
            this.TxID = txId;
            this.Args = args;
        }
        
        public string TxID { get; }
        public Dictionary<string, string> Args { get; }

        public TxArgumentsMessage Clone()
        {
            return new TxArgumentsMessage(
                txId: this.TxID,
                args: new Dictionary<string, string>(this.Args)
            );
        }
    }
}