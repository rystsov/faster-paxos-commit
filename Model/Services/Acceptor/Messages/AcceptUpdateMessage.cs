using Model.Infrastructure.AcceptorStorage;

namespace Model.Services.Acceptor.Messages
{
    public class AcceptUpdateMessage
    {
        public BallotNumber Ballot { get; }
        public TxState State { get; }

        public AcceptUpdateMessage(BallotNumber ballot, TxState state)
        {
            this.Ballot = ballot;
            this.State = state;
        }
    }
}