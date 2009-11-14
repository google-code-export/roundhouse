using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]

namespace roundhouse.console
{
    using System;
    using folders;
    using infrastructure;
    using infrastructure.containers;
    using infrastructure.filesystem;
    using infrastructure.logging;
    using log4net;
    using migrators;
    using resolvers;
    using runners;

    internal class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof (Program));

        private static void Main(string[] args)
        {
            ConfigurationPropertyHolder configuration = new ConsoleConfiguration(_logger);

            //todo: throw back the help text if there isn't any
            //todo: set configuration based on args

            ApplicationConfiguraton.set_defaults_if_properties_are_not_set(configuration);


            //todo: determine if this a call to the diff or the migrator
            run_migrator(configuration);

            if (!configuration.NonInteractive)
            {
                Console.Read();
            }
        }

        public static void run_migrator(ConfigurationPropertyHolder configuration)
        {
            IRunner roundhouse_runner = new RoundhouseMigrationRunner(
                configuration.RepositoryPath,
                Container.get_an_instance_of<environments.Environment>(),
                Container.get_an_instance_of<KnownFolders>(),
                Container.get_an_instance_of<FileSystemAccess>(),
                Container.get_an_instance_of<DatabaseMigrator>(),
                Container.get_an_instance_of<VersionResolver>()
                );
            try
            {
                roundhouse_runner.run();
            }
            catch (Exception exception)
            {
                Log.bound_to(typeof (Program)).
                    log_an_error_event_containing("{0} encountered an error:{1}{2}",
                                                  ApplicationParameters.name, 
                                                  Environment.NewLine, 
                                                  exception
                                                  );
                throw;
            }
        }
    }
}