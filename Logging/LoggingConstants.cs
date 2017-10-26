namespace Logging
{
    public class LoggingConstants
    {
        // Time interval in seconds for how often we check to see if it is time to write to the log file.
        public const int CheckInterval = 1;
        // Time interval in seconds for the maximum amount of time we will wait before writing to the log file.
        public const int LogInterval = 3;
        // Maximum number of items we can have in Queue before writing to the log file.
        public const long LogItemsUnloggedLimit = 500;
    }
}
