using System.Collections.Generic;

namespace Model.Services.Acceptor.Messages
{
    public class PrepareTxArgumentsMessage
    {
        public PrepareTxArgumentsMessage(string clientId, string txId, string txName, Dictionary<string, string> args, ISet<string> shardIDs)
        {
            this.TxID = txId;
            this.TxName = txName;
            this.Args = args;
            this.ShardIDs = shardIDs;
            this.ClientID = clientId;
        }
        
        public string ClientID { get; }
        public string TxID { get; }
        public string TxName { get; }
        public Dictionary<string, string> Args { get; }
        public ISet<string> ShardIDs { get; }

        public PrepareTxArgumentsMessage Clone()
        {
            return new PrepareTxArgumentsMessage(
                clientId: this.ClientID,
                txId: this.TxID,
                txName: this.TxName,
                args: new Dictionary<string, string>(this.Args),
                shardIDs: new HashSet<string>(this.ShardIDs)
            );
        }
    }
}