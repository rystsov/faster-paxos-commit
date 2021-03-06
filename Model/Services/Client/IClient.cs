﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Model.Services.Client.Exceptions;

namespace Model.Services.Client
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
        Task<Dictionary<string, string>> ExecuteTx(string name, ISet<string> keys, Dictionary<string, string> args, int timeoutMs);

        /// <summary>
        /// Aborts a transaction
        /// </summary>
        /// <param name="txId">Transactions's ID</param>
        /// <exception cref="AlreadyCommittedException" />
        /// <exception cref="TxUnknownException">Unknown error, see the TxID to fetch the tx's status</exception>
        Task AbortTx(string txId, int timeoutMs);

        /// <summary>
        /// A system may endup in a situation when a transaction is committed but the keys remain blocked
        /// In this case we need a way to mark a transaction committed and unblock the locks
        /// </summary>
        /// <exception cref="SomeException">In case of exception retry the request</exception>
        Task MarkCommitted(string txId, Dictionary<string, Dictionary<string, string>> keyValueUpdateByShard, int timeoutMs);
    }
}