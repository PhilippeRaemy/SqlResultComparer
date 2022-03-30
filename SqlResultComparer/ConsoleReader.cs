using System;
using System.Threading;

namespace SqlResultComparer
{
    internal static class ConsoleReader
    {
        private static readonly AutoResetEvent GetInput;
        private static readonly AutoResetEvent GotInput;
        private static string _input;

        static ConsoleReader()
        {
            GetInput = new AutoResetEvent(false);
            GotInput = new AutoResetEvent(false);
            var inputThread = new Thread(Reader) {IsBackground = true};
            inputThread.Start();
        }

        private static void Reader()
        {
            while (true)
            {
                GetInput.WaitOne();
                _input = Console.ReadLine();
                GotInput.Set();
                return;
            }
        }

        public static string ReadLine(int timeOutMillisecs, bool throwTimeoutException=true)
        {
            GetInput.Set();
            var success = GotInput.WaitOne(timeOutMillisecs);
            if (!success && throwTimeoutException)
            {
                throw new TimeoutException("User did not provide input within the timelimit.");
            }
            return _input;
        }
    }
}