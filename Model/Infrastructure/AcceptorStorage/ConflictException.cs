using System;

namespace Model.Infrastructure.AcceptorStorage
{
    public class ConflictException : Exception
    {
        public BallotNumber Future { get; }

        public ConflictException(BallotNumber future)
        {
            this.Future = future;
        }
    }
}