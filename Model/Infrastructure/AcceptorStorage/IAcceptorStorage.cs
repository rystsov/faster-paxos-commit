using System.Collections.Generic;
using System.Threading.Tasks;

namespace Model.Infrastructure.AcceptorStorage
{
    public interface IAcceptorStorage
    {
        // ok, conflict, unknown
        Task Commit(string txId, Dictionary<string, Dictionary<string, string>> shardKeyValueUpdates);
    }
}