using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class ExecutionAcceptedMessage
    {
        public string TxID { get; }
        public Dictionary<string, string> Result { get; }

        public ExecutionAcceptedMessage(string txId) : this(txId, new Dictionary<string, string>())
        {
        }
        
        public ExecutionAcceptedMessage(string txId, Dictionary<string, string> result)
        {
            this.TxID = txId;
            this.Result = result;
        }

        public ExecutionAcceptedMessage Clone()
        {
            return new ExecutionAcceptedMessage(
                this.TxID,
                new Dictionary<string, string>(this.Result)
            );
        }
    }
}