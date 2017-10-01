namespace Model.Infrastructure.AcceptorStorage
{
    public interface ITxExecutor
    {
        // unknown tx name
        // exception
        TxResult Execute(ITx tx);
    }
}