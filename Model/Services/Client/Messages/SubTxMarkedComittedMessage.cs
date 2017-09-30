namespace Model.Services.Client.Messages
{
    public class SubTxMarkedComittedMessage
    {
        public string ReqID { get; }

        public SubTxMarkedComittedMessage(string reqId)
        {
            this.ReqID = reqId;
        }

        public ExecutionAcceptedMessage Clone()
        {
            return new ExecutionAcceptedMessage(
                this.ReqID
            );
        }
    }
}