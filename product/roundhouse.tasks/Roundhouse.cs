namespace roundhouse.tasks
{
    using System;
    using System.Collections.Generic;
    using Castle.Windsor;
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
        [TaskAttribute("serverName", Required = true)]
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

        [TaskAttribute("revisionXmlFile", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string RevisionXmlFile { get; set; }

        public string RepositoryVersion { get; set; }

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

        [TaskAttribute("versionTableName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string VersionTableName { get; set; }

        [TaskAttribute("scriptsRunTableName", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string ScriptsRunTableName { get; set; }

        #endregion

        public void run_the_task()
        {
            Container.initialize_with(build_the_container());
            set_up_properties();

            infrastructure.logging.Log.bound_to(this).log_an_info_event_containing(
                "Executing {0} against contents of {1}.",
                ApplicationParameters.name,
                SqlFilesDirectory);
            
            IRunner roundhouse_runner = new RoundhouseRunner(
                                                RepositoryPath,
                                                RepositoryVersion,
                                                SqlFilesDirectory,
                                                UpFolderName,
                                                DownFolderName,
                                                RunFirstFolderName,
                                                FunctionsFolderName,
                                                ViewsFolderName,
                                                SprocsFolderName,
                                                PermissionsFolderName,
                                                Container.get_an_instance_of<FileSystemAccess>(),
                                                Container.get_an_instance_of<DatabaseMigrator>()
                                                );
            try
            {
                roundhouse_runner.run();
            }
            catch (Exception exception)
            {
                infrastructure.logging.Log.bound_to(this).log_an_error_event_containing("{0} encountered an error:{1}{2}", ApplicationParameters.name,
                                                                                        Environment.NewLine, exception);
                throw;
            }
        }

        private InversionContainer build_the_container()
        {
            IWindsorContainer windsor_container = new WindsorContainer();

            Logger nant_logger = new NAntLogger(this);
            Logger msbuild_logger = new MSBuildLogger(this, BuildEngine);
            Logger log4net_logger = new Log4NetLogger(the_logger);
            Logger multi_logger = new MultipleLogger(new List<Logger> { nant_logger, msbuild_logger, log4net_logger });

            windsor_container.Kernel.AddComponentInstance<Logger>(multi_logger);

            Database database_to_migrate = new SqlServerDatabase(ServerName, DatabaseName, ApplicationParameters.default_roundhouse_schema_name, VersionTableName, ScriptsRunTableName);
            windsor_container.Kernel.AddComponentInstance<Database>(database_to_migrate);

            windsor_container.AddComponent<FileSystemAccess, WindowsFileSystemAccess>();
            windsor_container.AddComponent<DatabaseMigrator, DefaultDatabaseMigrator>();
            windsor_container.AddComponent<LogFactory, MultipleLoggerLogFactory>();

            return new infrastructure.containers.custom.WindsorContainer(windsor_container);
        }

        public void set_up_properties()
        {
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
            if (string.IsNullOrEmpty(ScriptsRunTableName))
            {
                ScriptsRunTableName = ApplicationParameters.default_scripts_run_table_name;
            }
            if (string.IsNullOrEmpty(VersionTableName))
            {
                VersionTableName = ApplicationParameters.default_version_table_name;
            }
            if (string.IsNullOrEmpty(RevisionXmlFile))
            {
                RevisionXmlFile = ApplicationParameters.default_revision_xml_file;
            }

            RepositoryVersion = get_version_from_revision_file(RevisionXmlFile);
        }

        private string get_version_from_revision_file(string revision_xml_file)
        {
            //todo: xpath in the file
            return "0";
        }
    }
}