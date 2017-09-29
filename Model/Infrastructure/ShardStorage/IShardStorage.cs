using System.Collections.Generic;
using System.Threading.Tasks;

namespace Model.Infrastructure.ShardStorage
{
    public interface IShardStorage
    {
        /// <summary>
        /// Either blocks all the keys or throws an exception if any of the keys were blocked by other ongoing tx
        /// </summary>
        /// <param name="txId">ID of a transaction that initiated the lock</param>
        /// <param name="shardIDs">
        ///   IDs of shards participating in a transaction this information may be used 
        ///   to abort a transaction and rollback its locks keys
        /// </param>
        /// <param name="keys">Keys to block</param>
        /// <exception cref="AlreadyBlockedException">See ConflictingTXs for the list of conflicting TXs</exception>
        Task<Dictionary<string, string>> ReadAndBlock(string txId, string txName, ISet<string> shardIDs, ISet<string> keys);

        /// <summary>
        /// Rollback keys blocked by {txId} transaction 
        /// </summary>
        Task Unblock(string txId);

        Task UpdateAndUnblock(string txId, Dictionary<string, string> keyValueUpdate);
    }
}