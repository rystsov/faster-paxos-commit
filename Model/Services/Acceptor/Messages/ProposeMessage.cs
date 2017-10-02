using Model.Infrastructure.AcceptorStorage;

namespace Model.Services.Acceptor.Messages
{
    public class ProposeMessage
    {
        public BallotNumber Ballot { get; }

        public ProposeMessage(BallotNumber ballot)
        {
            this.Ballot = ballot;
        }
    }
}