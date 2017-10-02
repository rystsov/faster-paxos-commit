using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class AbortConfirmedMessage
    {
        public ISet<string> ShardIDs { get;  }

        public AbortConfirmedMessage(ISet<string> shardIDs)
        {
            this.ShardIDs = shardIDs;
        }

        public AbortConfirmedMessage Clone()
        {
            return new AbortConfirmedMessage(new HashSet<string>(this.ShardIDs));
        }
    }
}