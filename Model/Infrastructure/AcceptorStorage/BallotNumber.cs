namespace Model.Infrastructure.AcceptorStorage
{
    public class BallotNumber
    {
        public int Epoch { get; }
        public int Tick { get; }
        public int Proposer { get; }

        public BallotNumber(int epoch, int tick, int proposer)
        {
            this.Epoch = epoch;
            this.Tick = tick;
            this.Proposer = proposer;
        }

        public BallotNumber Inc()
        {
            return new BallotNumber(this.Epoch, this.Tick+1, this.Proposer);
        }

        public bool GreaterThan(BallotNumber bro)
        {
            if (this.Epoch > bro.Epoch) return true;
            if (this.Epoch < bro.Epoch) return false;
            if (this.Tick > bro.Tick) return true;
            if (this.Tick < bro.Tick) return false;
            if (this.Proposer > bro.Proposer) return true;
            return false;
        }
    }
}