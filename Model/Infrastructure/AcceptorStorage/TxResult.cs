using System.Collections.Generic;

namespace Model.Infrastructure.AcceptorStorage
{
    public class TxResult
    {
        public Dictionary<string, string> Result { get; }
        public Dictionary<string, Dictionary<string, string>> ShardKeyValueUpdates { get; }

        public TxResult()
        {
            this.Result = new Dictionary<string, string>();
            this.ShardKeyValueUpdates = new Dictionary<string, Dictionary<string, string>>();
        }
    }
}