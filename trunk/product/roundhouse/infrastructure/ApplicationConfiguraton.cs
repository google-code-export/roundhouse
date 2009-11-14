namespace roundhouse.infrastructure
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Security.Principal;
    using Castle.Windsor;
    using containers;
    using cryptography;
    using filesystem;
    using folders;
    using loaders;
    using logging;
    using logging.custom;
    using migrators;
    using resolvers;
    using sql;

    public static class ApplicationConfiguraton
    {
        public static void set_defaults_if_properties_are_not_set(ConfigurationPropertyHolder configuration_property_holder)
        {
            if (string.IsNullOrEmpty(configuration_property_holder.ServerName))
            {
                configuration_property_holder.ServerName = ApplicationParameters.default_server_name;
            }

            if (string.IsNullOrEmpty(configuration_property_holder.UpFolderName))
            {
                configuration_property_holder.UpFolderName = ApplicationParameters.default_up_folder_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.DownFolderName))
            {
                configuration_property_holder.DownFolderName = ApplicationParameters.default_down_folder_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.RunFirstFolderName))
            {
                configuration_property_holder.RunFirstFolderName = ApplicationParameters.default_run_first_folder_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.FunctionsFolderName))
            {
                configuration_property_holder.FunctionsFolderName = ApplicationParameters.default_functions_folder_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.ViewsFolderName))
            {
                configuration_property_holder.ViewsFolderName = ApplicationParameters.default_views_folder_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.SprocsFolderName))
            {
                configuration_property_holder.SprocsFolderName = ApplicationParameters.default_sprocs_folder_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.PermissionsFolderName))
            {
                configuration_property_holder.PermissionsFolderName = ApplicationParameters.default_permissions_folder_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.SchemaName))
            {
                configuration_property_holder.SchemaName = ApplicationParameters.default_roundhouse_schema_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.ScriptsRunTableName))
            {
                configuration_property_holder.ScriptsRunTableName = ApplicationParameters.default_scripts_run_table_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.VersionTableName))
            {
                configuration_property_holder.VersionTableName = ApplicationParameters.default_version_table_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.VersionFile))
            {
                configuration_property_holder.VersionFile = ApplicationParameters.default_version_file;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.VersionXPath))
            {
                configuration_property_holder.VersionXPath = ApplicationParameters.default_version_x_path;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.EnvironmentName))
            {
                configuration_property_holder.EnvironmentName = ApplicationParameters.default_environment_name;
            }
            if (string.IsNullOrEmpty(configuration_property_holder.OutputPath))
            {
                configuration_property_holder.OutputPath = ApplicationParameters.default_output_path;
            }
        }

        public static void build_the_container(ConfigurationPropertyHolder configuration_property_holder)
        {
            IWindsorContainer windsor_container = new WindsorContainer();

            windsor_container.AddComponent<LogFactory, MultipleLoggerLogFactory>();
            Logger nant_logger = new NAntLogger(configuration_property_holder.NAntTask);
            Logger msbuild_logger = new MSBuildLogger(configuration_property_holder, configuration_property_holder.MSBuildTask.BuildEngine);
            Logger log4net_logger = new Log4NetLogger(configuration_property_holder.Log4NetLogger);
            Logger multi_logger = new MultipleLogger(new List<Logger> { nant_logger, msbuild_logger, log4net_logger });
            windsor_container.Kernel.AddComponentInstance<Logger>(multi_logger);

            windsor_container.AddComponent<FileSystemAccess, WindowsFileSystemAccess>();

            var identity_of_runner = string.Empty;
            var windows_identity = WindowsIdentity.GetCurrent();
            if (windows_identity != null)
            {
                identity_of_runner = windows_identity.Name;
            }

            Assembly database_type_assembly = DefaultAssemblyLoader.load_assembly("roundhouse.databases.sqlserver2008");
            //Database resolved_database = (Database)database_type_assembly.CreateInstance("roundhouse.databases.sqlserver2008.SqlServerDatabase,roundhouse.databases.sqlserver2008");
            Database database_to_migrate = (Database)DefaultInstanceCreator.create_object_from_string_type("roundhouse.databases.sqlserver2008.SqlServerDatabase,roundhouse.databases.sqlserver2008");
            
            if (restore_from_file_ends_with_LiteSpeed_extension(configuration_property_holder.RestoreFromPath))
            {
                database_to_migrate = new SqlServerLiteSpeedDatabase(database_to_migrate);
            }
            database_to_migrate.server_name = configuration_property_holder.ServerName;
            database_to_migrate.database_name = configuration_property_holder.DatabaseName;
            database_to_migrate.roundhouse_schema_name = configuration_property_holder.SchemaName;
            database_to_migrate.version_table_name = configuration_property_holder.VersionTableName;
            database_to_migrate.scripts_run_table_name = configuration_property_holder.ScriptsRunTableName;
            database_to_migrate.user_name = identity_of_runner;

            windsor_container.Kernel.AddComponentInstance<Database>(database_to_migrate);

            CryptographicService crypto_provider = new MD5CryptographicService();

            DatabaseMigrator database_migrator = new DefaultDatabaseMigrator(windsor_container.Resolve<Database>(), crypto_provider,
                                                                             configuration_property_holder.Restore, configuration_property_holder.RestoreFromPath, configuration_property_holder.OutputPath, !configuration_property_holder.WarnOnOneTimeScriptChanges);
            windsor_container.Kernel.AddComponentInstance<DatabaseMigrator>(database_migrator);

            VersionResolver xml_version_finder = new XmlFileVersionResolver(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.VersionXPath, configuration_property_holder.VersionFile);
            VersionResolver dll_version_finder = new DllFileVersionResolver(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.VersionFile);
            IEnumerable<VersionResolver> resolvers = new List<VersionResolver> { xml_version_finder, dll_version_finder };
            VersionResolver version_finder = new ComplexVersionResolver(resolvers);
            windsor_container.Kernel.AddComponentInstance<VersionResolver>(version_finder);

            Folder up_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.SqlFilesDirectory, configuration_property_holder.UpFolderName, true);
            Folder down_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.SqlFilesDirectory, configuration_property_holder.DownFolderName, true);
            Folder run_first_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.SqlFilesDirectory, configuration_property_holder.RunFirstFolderName, false);
            Folder functions_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.SqlFilesDirectory, configuration_property_holder.FunctionsFolderName, false);
            Folder views_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.SqlFilesDirectory, configuration_property_holder.ViewsFolderName, false);
            Folder sprocs_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.SqlFilesDirectory, configuration_property_holder.SprocsFolderName, false);
            Folder permissions_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), configuration_property_holder.SqlFilesDirectory, configuration_property_holder.PermissionsFolderName, false);

            KnownFolders known_folders = new DefaultKnownFolders(up_folder, down_folder, run_first_folder, functions_folder, views_folder, sprocs_folder, permissions_folder);
            windsor_container.Kernel.AddComponentInstance<KnownFolders>(known_folders);

            environments.Environment environment = new environments.DefaultEnvironment(configuration_property_holder.EnvironmentName);
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