using System.Collections.Generic;

namespace Model.Infrastructure
{
    public interface IServiceLocator
    {
        string GetShardIdByKey(string key);
        ISet<string> GetAcceptorIDs();
    }
}