﻿namespace roundhouse.tasks
{
    using System;
    using folders;
    using infrastructure;
    using infrastructure.containers;
    using infrastructure.filesystem;
    using log4net;
    using Microsoft.Build.Framework;
    using migrators;
    using NAnt.Core;
    using NAnt.Core.Attributes;
    using resolvers;
    using runners;
    using Environment=roundhouse.environments.Environment;

    [TaskName("roundhouse")]
    public class Roundhouse : Task, ITask, ConfigurationPropertyHolder
    {
        private readonly ILog the_logger = LogManager.GetLogger(typeof (Roundhouse));

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

        public ILog Log4NetLogger
        {
            get { return the_logger; }
        }

        public Task NAntTask
        {
            get { return this; }
        }

        public ITask MSBuildTask
        {
            get { return this; }
        }

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

        [TaskAttribute("nonInteractive", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public bool NonInteractive { get; set; }

        #endregion

        public void run_the_task()
        {
            ApplicationConfiguraton.set_defaults_if_properties_are_not_set(this);

            if (Restore && string.IsNullOrEmpty(RestoreFromPath))
            {
                throw new Exception(
                    "If you set Restore to true, you must specify a location for the database to be restored from. That property is RestoreFromPath in MSBuild and restoreFromPath in NAnt.");
            }
            ApplicationConfiguraton.build_the_container(this);

            infrastructure.logging.Log.bound_to(this).log_an_info_event_containing(
                "Executing {0} against contents of {1}.",
                ApplicationParameters.name,
                SqlFilesDirectory);

            IRunner roundhouse_runner = new RoundhouseMigrationRunner(
                RepositoryPath,
                Container.get_an_instance_of<Environment>(),
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
                infrastructure.logging.Log.bound_to(this).
                    log_an_error_event_containing("{0} encountered an error:{1}{2}",
                                                  ApplicationParameters.name, 
                                                  System.Environment.NewLine, 
                                                  exception
                                                  );
                throw;
            }
        }
    }
}