using System.Xml;
using log4net;
using log4net.Config;

namespace SupportBackend
{
    public static class Logger
    {
        private class State
        {
            public State()
            {
                Enabled = false;
            }

            public bool Enabled { get; set; }
        }

        private enum LogLevel
        {
            Info,
            Debug,
            Warn,
            Error
        }

        private static readonly ILog logger = LogManager.GetLogger("SupportLogger");
        private static readonly State loggerState = new State();
        private static readonly State info = new State();
        private static readonly State debug = new State();
        private static readonly State warn = new State();
        private static readonly State error = new State();

        private static void InitLogger()
        {
            if (!loggerState.Enabled)
            {
                XmlDocument log4netConfig = new XmlDocument();

                FileStream fs = File.OpenRead("log4net.config");
                log4netConfig.Load(fs);
                XmlConfigurator.Configure(log4netConfig["log4net"]);
                logger.Info("Log System Initialized");
                fs.Dispose();

                // TODO : вынести в конфиг настройки уровней логирования
                info.Enabled = true;
                debug.Enabled = true;
                warn.Enabled = true;
                error.Enabled = true;

                loggerState.Enabled = true;
            }
        }

        private static string ConcatMessageLog(string message, string operationName)
        {
            return string.Format("[{0}] {1}", operationName, message);
        }

        private static void Log(string message, string operationName, LogLevel logLevel)
        {
            InitLogger();
            string msg2log = ConcatMessageLog(message, operationName);

            if (logLevel == LogLevel.Info && info.Enabled)
            {
                logger.Info(msg2log);
            }
            if (logLevel == LogLevel.Debug && debug.Enabled)
            {
                logger.Debug(msg2log);
            }
            if (logLevel == LogLevel.Warn && warn.Enabled)
            {
                logger.Warn(msg2log);
            }
            if (logLevel == LogLevel.Error && error.Enabled)
            {
                logger.Error(msg2log);
            }
        }

        public static void Info(string message, string operationName)
        {
            Log(message, operationName, LogLevel.Info);
        }

        public static void Debug(string message, string operationName)
        {
            Log(message, operationName, LogLevel.Debug);
        }

        public static void Warn(string message, string operationName)
        {
            Log(message, operationName, LogLevel.Warn);
        }

        public static void Error(string message, string operationName)
        {
            Log(message, operationName, LogLevel.Error);
        }
    }
}