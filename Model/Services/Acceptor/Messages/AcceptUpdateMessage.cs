using Model.Infrastructure.AcceptorStorage;

namespace Model.Services.Acceptor.Messages
{
    public class AcceptUpdateMessage
    {
        public string ReqID { get; }
        public string TxID { get; }
        public BallotNumber Ballot { get; }
        public TxState State { get; }

        public AcceptUpdateMessage(string reqId, string txId, BallotNumber ballot, TxState state)
        {
            this.ReqID = reqId;
            this.TxID = txId;
            this.Ballot = ballot;
            this.State = state;
        }
    }
}