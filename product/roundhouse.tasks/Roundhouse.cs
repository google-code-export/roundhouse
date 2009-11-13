using System.IO;
using roundhouse.folders;

namespace roundhouse.tasks
{
    using System;
    using System.Collections.Generic;
    using Castle.Windsor;
    using cryptography;
    using infrastructure;
    using infrastructure.containers;
    using infrastructure.filesystem;
    using infrastructure.logging;
    using infrastructure.logging.custom;
    using log4net;
    using Microsoft.Build.Framework;
    using migrators;
    using NAnt.Core;
    using NAnt.Core.Attributes;
    using resolvers;
    using runners;
    using sql;

    [TaskName("roundhouse")]
    public class Roundhouse : Task, ITask
    {
        private readonly ILog the_logger = LogManager.GetLogger(typeof(Roundhouse));

        #region MSBuild

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }

        /// <summary>
        /// The function for the MSBuild task that actually does the task
        /// </summary>
        /// <returns>true if the task is successful</returns>
        bool ITask.Execute()
        {
            run_the_task();
            return true;
        }

        #endregion

        #region NAnt

        /// <summary>
        /// Executes the NAnt task
        /// </summary>
        protected override void ExecuteTask()
        {
            run_the_task();
        }

        #endregion

        #region Properties

        [Required]
        [TaskAttribute("serverName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string ServerName { get; set; }

        [Required]
        [TaskAttribute("databaseName", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string DatabaseName { get; set; }

        [Required]
        [TaskAttribute("sqlFilesDirectory", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string SqlFilesDirectory { get; set; }

        [TaskAttribute("repositoryPath", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string RepositoryPath { get; set; }

        [TaskAttribute("versionFile", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string VersionFile { get; set; }

        [TaskAttribute("versionXPath", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string VersionXPath { get; set; }

        [TaskAttribute("upFolderName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string UpFolderName { get; set; }

        [TaskAttribute("downFolderName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string DownFolderName { get; set; }

        [TaskAttribute("runFirstFolderName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string RunFirstFolderName { get; set; }

        [TaskAttribute("functionsFolderName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string FunctionsFolderName { get; set; }

        [TaskAttribute("viewsFolderName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string ViewsFolderName { get; set; }

        [TaskAttribute("sprocsFolderName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string SprocsFolderName { get; set; }

        [TaskAttribute("permissionsFolderName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string PermissionsFolderName { get; set; }

        [TaskAttribute("schemaName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string SchemaName { get; set; }

        [TaskAttribute("versionTableName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string VersionTableName { get; set; }

        [TaskAttribute("scriptsRunTableName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string ScriptsRunTableName { get; set; }

        [TaskAttribute("environmentName", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public string EnvironmentName { get; set; }

        [TaskAttribute("restore", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public bool Restore { get; set; }

        [TaskAttribute("restoreFromPath", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public string RestoreFromPath { get; set; }

        [TaskAttribute("outputPath", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public string OutputPath { get; set; }

        [TaskAttribute("warnOnOneTimeScriptChanges", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public bool WarnOnOneTimeScriptChanges { get; set; }

        #endregion

        public void run_the_task()
        {
            set_up_properties();
            if (Restore && string.IsNullOrEmpty(RestoreFromPath))
            {
                throw new Exception("If you set Restore to true, you must specify a location for the database to be restored from. That property is RestoreFromPath in MSBuild and restoreFromPath in NAnt.");
            }
            Container.initialize_with(build_the_container());

            infrastructure.logging.Log.bound_to(this).log_an_info_event_containing(
                "Executing {0} against contents of {1}.",
                ApplicationParameters.name,
                SqlFilesDirectory);

            IRunner roundhouse_runner = new RoundhouseRunner(
                                                RepositoryPath,
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
                infrastructure.logging.Log.
                    bound_to(this).
                    log_an_error_event_containing("{0} encountered an error:{1}{2}",
                    ApplicationParameters.name, Environment.NewLine, exception);
                throw;
            }
        }

        private InversionContainer build_the_container()
        {
            IWindsorContainer windsor_container = new WindsorContainer();

            windsor_container.AddComponent<LogFactory, MultipleLoggerLogFactory>();
            Logger nant_logger = new NAntLogger(this);
            Logger msbuild_logger = new MSBuildLogger(this, BuildEngine);
            Logger log4net_logger = new Log4NetLogger(the_logger);
            Logger multi_logger = new MultipleLogger(new List<Logger> { nant_logger, msbuild_logger, log4net_logger });
            windsor_container.Kernel.AddComponentInstance<Logger>(multi_logger);

            windsor_container.AddComponent<FileSystemAccess, WindowsFileSystemAccess>();

            CryptographicService crypto_provider = new MD5CryptographicService();

            Database database_to_migrate = new SqlServerDatabase(ServerName, DatabaseName, SchemaName, VersionTableName, ScriptsRunTableName, crypto_provider);
            
            if (restore_from_file_ends_with_LiteSpeed_extension(RestoreFromPath))
            {
                database_to_migrate = new SqlServerLiteSpeedDatabase(database_to_migrate);
            }
            windsor_container.Kernel.AddComponentInstance<Database>(database_to_migrate);

            DatabaseMigrator database_migrator = new DefaultDatabaseMigrator(windsor_container.Resolve<Database>(),
                                                                             Restore, RestoreFromPath,OutputPath,!WarnOnOneTimeScriptChanges);
            windsor_container.Kernel.AddComponentInstance<DatabaseMigrator>(database_migrator);

            VersionResolver xml_version_finder = new XmlFileVersionResolver(windsor_container.Resolve<FileSystemAccess>(), VersionXPath, VersionFile);
            VersionResolver dll_version_finder = new DllFileVersionResolver(windsor_container.Resolve<FileSystemAccess>(), VersionFile);
            IEnumerable<VersionResolver> resolvers = new List<VersionResolver> { xml_version_finder, dll_version_finder };
            VersionResolver version_finder = new ComplexVersionResolver(resolvers);
            windsor_container.Kernel.AddComponentInstance<VersionResolver>(version_finder);

            Folder up_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), SqlFilesDirectory, UpFolderName, true);
            Folder down_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), SqlFilesDirectory, DownFolderName, true);
            Folder run_first_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), SqlFilesDirectory, RunFirstFolderName, false);
            Folder functions_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), SqlFilesDirectory, FunctionsFolderName, false);
            Folder views_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), SqlFilesDirectory, ViewsFolderName, false);
            Folder sprocs_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), SqlFilesDirectory, SprocsFolderName, false);
            Folder permissions_folder = new DefaultFolder(windsor_container.Resolve<FileSystemAccess>(), SqlFilesDirectory, PermissionsFolderName, false);

            KnownFolders known_folders = new DefaultKnownFolders(up_folder, down_folder, run_first_folder, functions_folder, views_folder, sprocs_folder, permissions_folder);
            windsor_container.Kernel.AddComponentInstance<KnownFolders>(known_folders);

            environments.Environment environment = new environments.DefaultEnvironment(EnvironmentName);
            windsor_container.Kernel.AddComponentInstance<environments.Environment>(environment);

            return new infrastructure.containers.custom.WindsorContainer(windsor_container);
        }

        private static bool restore_from_file_ends_with_LiteSpeed_extension(string restore_path)
        {
            if (string.IsNullOrEmpty(restore_path)) return false;

            return Path.GetFileNameWithoutExtension(restore_path).ToLower().EndsWith("ls");
        }

        public void set_up_properties()
        {
            if (string.IsNullOrEmpty(ServerName))
            {
                ServerName = ApplicationParameters.default_server_name;
            }

            if (string.IsNullOrEmpty(UpFolderName))
            {
                UpFolderName = ApplicationParameters.default_up_folder_name;
            }
            if (string.IsNullOrEmpty(DownFolderName))
            {
                DownFolderName = ApplicationParameters.default_down_folder_name;
            }
            if (string.IsNullOrEmpty(RunFirstFolderName))
            {
                RunFirstFolderName = ApplicationParameters.default_run_first_folder_name;
            }
            if (string.IsNullOrEmpty(FunctionsFolderName))
            {
                FunctionsFolderName = ApplicationParameters.default_functions_folder_name;
            }
            if (string.IsNullOrEmpty(ViewsFolderName))
            {
                ViewsFolderName = ApplicationParameters.default_views_folder_name;
            }
            if (string.IsNullOrEmpty(SprocsFolderName))
            {
                SprocsFolderName = ApplicationParameters.default_sprocs_folder_name;
            }
            if (string.IsNullOrEmpty(PermissionsFolderName))
            {
                PermissionsFolderName = ApplicationParameters.default_permissions_folder_name;
            }
            if (string.IsNullOrEmpty(SchemaName))
            {
                SchemaName = ApplicationParameters.default_roundhouse_schema_name;
            } 
            if (string.IsNullOrEmpty(ScriptsRunTableName))
            {
                ScriptsRunTableName = ApplicationParameters.default_scripts_run_table_name;
            }
            if (string.IsNullOrEmpty(VersionTableName))
            {
                VersionTableName = ApplicationParameters.default_version_table_name;
            }
            if (string.IsNullOrEmpty(VersionFile))
            {
                VersionFile = ApplicationParameters.default_version_file;
            }
            if (string.IsNullOrEmpty(VersionXPath))
            {
                VersionXPath = ApplicationParameters.default_version_x_path;
            }
            if (string.IsNullOrEmpty(EnvironmentName))
            {
                EnvironmentName = ApplicationParameters.default_environment_name;
            }
            if (string.IsNullOrEmpty(OutputPath))
            {
                OutputPath = ApplicationParameters.default_output_path;
            }
        }
    }
}