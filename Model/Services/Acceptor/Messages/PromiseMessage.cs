using Model.Infrastructure.AcceptorStorage;

namespace Model.Services.Acceptor.Messages
{
    public class PromiseMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public BallotNumber Ballot { get; }

        public PromiseMessage(string reqId, string txId, BallotNumber ballot)
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.Ballot = ballot;
        }

        public PromiseMessage Clone()
        {
            return this;
        }
    }
}