namespace roundhouse.infrastructure
{
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Principal;
    using Castle.Windsor;
    using containers;
    using cryptography;
    using filesystem;
    using folders;
    using logging;
    using logging.custom;
    using migrators;
    using resolvers;
    using sql;

    public static class ApplicationConfiguraton
    {
        public static void build_the_container(ConfigurationPropertyHolder property_holder)
        {
            IWindsorContainer windsor_container = new WindsorContainer();

            windsor_container.AddComponent<LogFactory, MultipleLoggerLogFactory>();
            Logger nant_logger = new NAntLogger(property_holder.NAntTask);
            Logger msbuild_logger = new MSBuildLogger(property_holder, property_holder.MSBuildTask.BuildEngine);
            Logger log4net_logger = new Log4NetLogger(property_holder.Log4NetLogger);
            Logger multi_logger = new MultipleLogger(new List<Logger> { nant_logger, msbuild_logger, log4net_logger });
            windsor_container.Kernel.AddComponentInstance<Logger>(multi_logger);

            windsor_container.AddComponent<FileSystemAccess, WindowsFileSystemAccess>();

            var identity_of_runner = string.Empty;
            var windows_identity = WindowsIdentity.GetCurrent();
            if (windows_identity != null)
            {
                identity_of_runner = windows_identity.Name;
            }

            Database database_to_migrate = new SqlServerDatabase();

            if (restore_from_file_ends_with_LiteSpeed_extension(property_holder.RestoreFromPath))
            {
                database_to_migrate = new SqlServerLiteSpeedDatabase(database_to_migrate);
            }
            database_to_migrate.server_name = property_holder.ServerName;
            database_to_migrate.database_name = property_holder.DatabaseName;
            database_to_migrate.roundhouse_schema_name = property_holder.SchemaName;
            database_to_migrate.version_table_name = property_holder.VersionTableName;
            database_to_migrate.scripts_run_table_name = property_holder.ScriptsRunTableName;
            database_to_migrate.user_name = identity_of_runner;

            windsor_container.Kernel.AddComponentInstance<Database>(database_to_migrate);

            CryptographicService crypto_provider = new MD5CryptographicService();

            DatabaseMigrator database_migrator = new DefaultDatabaseMigrator(windsor_container.Resolve<Database>(), crypto_provider,
                                                                             property_holder.Restore, property_holder.RestoreFromPath, property_holder.OutputPath, !property_holder.WarnOnOneTimeScriptChanges);
            windsor_container.Kernel.AddComponentInstance<DatabaseMigrator>(database_migrator);

            VersionResolver xml_version_finder = new XmlFileVersionResolver(windsor_container.Resolve<FileSystemAccess>(), property_holder.VersionXPath, property_holder.VersionFile);
            VersionResolver dll_version_finder = new DllFileVersionResolver(windsor_container.Resolve<FileSystemAccess>(), property_holder.VersionFile);
            IEnumerable<VersionResolver> resolvers = new List<VersionResolver> { xml_version_finder, dll_version_finder };
            VersionResolver version_finder = new ComplexVersionResolver(resolvers);
            windsor_container.Kernel.AddComponentInstance<VersionResolver>(version_finder);

            Folder up_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), property_holder.SqlFilesDirectory, property_holder.UpFolderName, true);
            Folder down_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), property_holder.SqlFilesDirectory, property_holder.DownFolderName, true);
            Folder run_first_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), property_holder.SqlFilesDirectory, property_holder.RunFirstFolderName, false);
            Folder functions_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), property_holder.SqlFilesDirectory, property_holder.FunctionsFolderName, false);
            Folder views_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), property_holder.SqlFilesDirectory, property_holder.ViewsFolderName, false);
            Folder sprocs_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), property_holder.SqlFilesDirectory, property_holder.SprocsFolderName, false);
            Folder permissions_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), property_holder.SqlFilesDirectory, property_holder.PermissionsFolderName, false);

            KnownFolders known_folders = new DefaultKnownFolders(up_folder, down_folder, run_first_folder, functions_folder, views_folder, sprocs_folder, permissions_folder);
            windsor_container.Kernel.AddComponentInstance<KnownFolders>(known_folders);

            environments.Environment environment = new environments.DefaultEnvironment(property_holder.EnvironmentName);
            windsor_container.Kernel.AddComponentInstance<environments.Environment>(environment);


            Container.initialize_with(new containers.custom.WindsorContainer(windsor_container));
        }

        private static bool restore_from_file_ends_with_LiteSpeed_extension(string restore_path)
        {
            if (string.IsNullOrEmpty(restore_path)) return false;

            return Path.GetFileNameWithoutExtension(restore_path).ToLower().EndsWith("ls");
        }
    }
}