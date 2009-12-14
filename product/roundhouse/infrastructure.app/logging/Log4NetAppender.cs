using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;

namespace roundhouse.infrastructure.app.logging
{
    public class Log4NetAppender
    {
        private static ILog _logger = LogManager.GetLogger(typeof (Log4NetAppender));
        //public static IEnumerable<IAppender> configure_appenders_with(ConfigurationPropertyHolder configuration)
        //{
        //    IList<IAppender> appenders = new List<IAppender>();
        //    appenders.Add(set_up_rolling_file_appender(configuration));
        //    appenders.Add(set_up_console_appender(configuration));

        //    return appenders;
        //}

        private static IAppender set_up_console_appender(ConfigurationPropertyHolder configuration)
        {
            _logger.Warn("Setting up console");
            ConsoleAppender appender = new ConsoleAppender();
            appender.Name = "ConsoleAppender";
            
            PatternLayout pattern_layout = new PatternLayout("%message%newline");
            pattern_layout.ActivateOptions();
            appender.Layout = pattern_layout;
            
            appender.ActivateOptions();

            return appender;
        }

        private static IAppender set_up_rolling_file_appender(ConfigurationPropertyHolder configuration)
        {
            string file_name = configuration.OutputPath + "\\RoundhousE.Changes.log";

            RollingFileAppender appender = new RollingFileAppender();
            appender.Name = "RollingLogFileAppender";
            appender.File = file_name;
            appender.AppendToFile = false;
            appender.StaticLogFileName = true;

            PatternLayout pattern_layout= new PatternLayout("%date [%-5level] - %message%newline");
            pattern_layout.ActivateOptions();
            appender.Layout = pattern_layout;

            appender.ActivateOptions();

            return appender;
        }

        public static void configure(ConfigurationPropertyHolder configuration)
        {
            ILoggerRepository log_repository = LogManager.GetRepository(Assembly.GetCallingAssembly());
            log_repository.Threshold = Level.Info;
            
            BasicConfigurator.Configure(log_repository, set_up_console_appender(configuration));
            BasicConfigurator.Configure(log_repository,set_up_rolling_file_appender(configuration));
            
            //log4net.Repository.Hierarchy.Logger roundhouse_logger = LoggerManager.CreateRepository(
        }

    }
}