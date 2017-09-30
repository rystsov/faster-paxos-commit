namespace Model.Services.Client.Messages
{
    public class SubTxMarkedComittedMessage
    {
        public string ReqID { get; }

        public SubTxMarkedComittedMessage(string reqId)
        {
            this.ReqID = reqId;
        }

        public ExecutionCommittedMessage Clone()
        {
            return new ExecutionCommittedMessage(
                this.ReqID
            );
        }
    }
}