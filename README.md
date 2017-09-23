# faster-paxos-commit
Non-blocking 2PC transactions between key/value storages in 1.5 round trip time

The algorithm is a combination of 2-phase-commit and Paxos (see the [Consensus on Transaction Commit](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/tr-2003-96.pdf) paper by Lamport & Gray) with a couple optimizations.

Overview of the happy case:

  1. A client sends a transaction (a name of a stored procedure, a list of the arguments and a list of keys, it reads or modifies) to all shards (services co-located with the storages)

  2. Each shard reads the keys of the transaction it is responsible for, blocks them and broadcast to a proposer

  3. The proposer waits for the messages from all shards participating in the transaction, sends the prepare message to the acceptors and waits for the majority of the responses

  4. Then it executes a stored proc and sends the outcome as an accept message to the acceptors

  5. Once an acceptor accepts the outcome it broadcasts it to the client and to the shards

  6. When a client receives the broadcast from the majority of the acceptors it considers the transaction to be committed

  7. When a shard receives the broadcast from the majority of the acceptors it updates the values and unblocks them

## Hey, you're a liar it's 3 RTT not 1.5 RTT!

Yes, but it's fixable.

If we agree to use a fixed ballot number when we try to commit a transaction for the first time then we can pretend that the prepare step has been already executed, so now it's 2 RTT.

The second observation is since we fixed the ballot number the proposer doesn't add any new information to the dataflow so we can eliminate it, make shards send data directly to the acceptors and put tx execution logic there without affecting the behaviour of the system, so it's the promised 1.5 RTT.

## What's the unhappy case?

Once a client runs into a blocked record, they read the status of the tx from the acceptors using the standard prepare/accept Paxos protocol and then either try to finish the blocking tx or abort it.