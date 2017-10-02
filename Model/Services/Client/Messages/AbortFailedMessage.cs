using System.Collections.Generic;

namespace Model.Services.Client.Messages
{
    public class AbortFailedMessage
    {
        public Dictionary<string, Dictionary<string, string>> ShardKeyValueUpdate { get; }

        public AbortFailedMessage(
            Dictionary<string, Dictionary<string, string>> shardKeyValueUpdate
        )
        {
            this.ShardKeyValueUpdate = shardKeyValueUpdate;
        }

        public AbortFailedMessage Clone()
        {
            var keyValueUpdateByShard = new Dictionary<string, Dictionary<string, string>>();
            foreach (var key in this.ShardKeyValueUpdate.Keys)
            {
                keyValueUpdateByShard.Add(key, new Dictionary<string, string>(this.ShardKeyValueUpdate[key]));
            }
            
            return new AbortFailedMessage(
                keyValueUpdateByShard
            );
        }
    }
}