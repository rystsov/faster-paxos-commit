namespace Model.Infrastructure.AcceptorStorage
{
    public class AcceptedValue
    {
        public BallotNumber PromisedBallot { get; }
        public BallotNumber AcceptedBallot { get; }
        public TxState State { get; }

        public AcceptedValue(BallotNumber promisedBallot, BallotNumber acceptedBallot, TxState state)
        {
            this.PromisedBallot = promisedBallot;
            this.AcceptedBallot = acceptedBallot;
            this.State = state;
        }
    }
}