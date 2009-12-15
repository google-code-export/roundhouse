namespace roundhouse.infrastructure
{
    using log4net;
    using Microsoft.Build.Framework;
    using NAnt.Core;

    public interface ConfigurationPropertyHolder
    {
        ITask MSBuildTask { get; }
        Task NAntTask { get; }
        ILog Log4NetLogger { get; }

        string ServerName { get; set; }
        string DatabaseName { get; set; }
        string SqlFilesDirectory { get; set; }
        string RepositoryPath { get; set; }
        string VersionFile { get; set; }
        string VersionXPath { get; set; }
        string UpFolderName { get; set; }
        string DownFolderName { get; set; }
        string RunFirstAfterUpFolderName { get; set; }
        string FunctionsFolderName { get; set; }
        string ViewsFolderName { get; set; }
        string SprocsFolderName { get; set; }
        string PermissionsFolderName { get; set; }
        string SchemaName { get; set; }
        string VersionTableName { get; set; }
        string ScriptsRunTableName { get; set; }
        string EnvironmentName { get; set; }
        bool Restore { get; set; }
        string RestoreFromPath { get; set; }
        string OutputPath { get; set; }
        bool WarnOnOneTimeScriptChanges { get; set; }
        bool NonInteractive { get; set; }
        string DatabaseType { get; set; }
        bool Drop { get; set; }
        bool WithTransaction { get; set; }
        bool RecoveryModeSimple { get; set; }
        bool Debug { get; set; }
    }
}