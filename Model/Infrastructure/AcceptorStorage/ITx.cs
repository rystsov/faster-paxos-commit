using System.Collections.Generic;

namespace Model.Infrastructure.AcceptorStorage
{
    public interface ITx
    {
        string TxID { get; }
        string TxName { get; }
        Dictionary<string, string> Args { get; }
        ISet<string> ShardIDs { get; }
        Dictionary<string, Dictionary<string, string>> ShardKeyValue { get; }
    }
}