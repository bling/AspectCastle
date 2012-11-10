using System;
using Castle.Core.Logging;

namespace AspectCastle.Tests
{
    public static class TestUtil
    {
        /// <summary>Creates mocked out loggers where every logger level is enabled.</summary>
        /// <remarks>This is mainly to force all anonymous delegates to minimize false positives for low code coverage.</remarks>
        public static ILogger CreateLogger()
        {
            return new MockLogger();
        }

        public static ILogger CreateLogger(Action<string> messageReceiver)
        {
            return new MockLogger(messageReceiver);
        }

        private class MockLogger : LevelFilteredLogger
        {
            private readonly Action<string> receiver;

            public MockLogger(Action<string> receiver = null)
                : base(LoggerLevel.Debug)
            {
                this.receiver = receiver ?? (_ => { });
            }

            public override ILogger CreateChildLogger(string loggerName)
            {
                return this;
            }

            protected override void Log(LoggerLevel loggerLevel, string loggerName, string message, Exception exception)
            {
                this.receiver(message);
            }
        }
    }
}