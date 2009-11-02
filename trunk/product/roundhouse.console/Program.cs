using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

namespace roundhouse.console
{
    using log4net;

    internal class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            _logger.Info("HI!");
            System.Console.Read();
        }
    }
}