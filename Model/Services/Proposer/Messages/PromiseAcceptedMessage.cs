using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Model.Infrastructure.AcceptorStorage;

namespace Model.Services.Proposer.Messages
{
    public class PromiseAcceptedMessage
    {
        public BallotNumber PrevBallot { get; }
        public TxState State { get; }
        
        public PromiseAcceptedMessage(BallotNumber prevBallot, TxState state)
        {
            this.PrevBallot = prevBallot;
            this.State = state;
        }

        public PromiseAcceptedMessage Clone()
        {
            return new PromiseAcceptedMessage(this.PrevBallot, this.State.Clone());
        }
    }
}