using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Model.Client
{
    public interface IClient
    {
        /// <summary>
        /// Executes a transaction.
        /// </summary>
        /// <param name="name">Name of the transaction</param>
        /// <param name="keys">Keys which a transaction reads or modifies</param>
        /// <param name="args">Additional arguments for a transaction</param>
        /// <returns>A hashmap formed by a teh stored procedure</returns>
        /// <exception cref="TxUnknownException">Unknown error, see the TxID to fetch the tx's status</exception>
        /// <exception cref="TxConflictException">See ConflictingTXs for the list of conflicting TXs</exception>
        /// <exception cref="TimeoutException" />
        Task<Dictionary<string, string>> ExecuteTx(string name, ISet<string> keys, Dictionary<string, string> args, int timeoutMs);

        /// <summary>
        /// Fetches a transactions status
        /// </summary>
        /// <param name="txId">Transactions's ID</param>
        /// <returns>Status</returns>
        /// <exception cref="SomeException">In case of exception retry the request</exception>
        Task<TxStatus> FetchTxStatus(string txId);

        /// <summary>
        /// Aborts a transaction
        /// </summary>
        /// <param name="txId">Transactions's ID</param>
        /// <exception cref="AlreadyCommittedException" />
        /// <exception cref="TxUnknownException">Unknown error, see the TxID to fetch the tx's status</exception>
        Task AbortTx(string txId);
    }
}