using Model.Infrastructure.AcceptorStorage;

namespace Model.Services.Proposer.Messages
{
    public class PromiseConflictedMessage
    {
        public BallotNumber Future { get; }

        public PromiseConflictedMessage(BallotNumber future)
        {
            this.Future = future;
        }
    }
}