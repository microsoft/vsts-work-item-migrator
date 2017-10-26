namespace Logging
{
    /// <summary>
    /// This is a static class instead of an enum so that we can pass into Log Methods with casting to int.
    /// We cannot change parameter of these methods because it is not our code.
    /// </summary>
    public static class LogDestination
    {
        public const int All = 0;
        public const int File = 1;
        public const int Console = 2;
    }
}
