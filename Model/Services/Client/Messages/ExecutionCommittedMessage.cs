using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class ExecutionCommittedMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> Result { get; }

        public ExecutionCommittedMessage(string txId) : this(txId, new Dictionary<string, string>())
        {
        }
        
        public ExecutionCommittedMessage(string txId, Dictionary<string, string> result)
        {
            this.TxID = txId;
            this.Result = result;
        }

        public ExecutionCommittedMessage Clone()
        {
            return new ExecutionCommittedMessage(
                this.TxID,
                new Dictionary<string, string>(this.Result)
            );
        }
    }
}