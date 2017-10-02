using System.Threading.Tasks;
using Model.Infrastructure.AcceptorStorage;

namespace Model.Infrastructure.ProposerStorage
{
    public interface IProposerStorage
    {
        Task<BallotNumber> LoadBallotNumber();
        Task<BallotNumber> FastForward(BallotNumber eFuture);
    }
}