using System;

namespace Common.Migration
{
    public class MigrationException : Exception
    {
        public MigrationException() : base() { }
        public MigrationException(string message) : base(message) { }
        public MigrationException(string message, Exception inner) : base(message, inner) { }
    }
}
