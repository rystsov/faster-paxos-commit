using Model.Infrastructure.AcceptorStorage;

namespace Model.Services.Acceptor.Messages
{
    public class PromiseMessage
    {
        public string TxID { get; }
        public BallotNumber Ballot { get; }

        public PromiseMessage(string txId, BallotNumber ballot)
        {
            this.TxID = txId;
            this.Ballot = ballot;
        }

        public PromiseMessage Clone()
        {
            return this;
        }
    }
}