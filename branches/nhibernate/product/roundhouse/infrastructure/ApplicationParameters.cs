namespace roundhouse.infrastructure
{
    public static class ApplicationParameters
    {
        public static string name = "RoundhousE";
        // defaults
        public readonly static string default_up_folder_name = "up";
        public readonly static string default_down_folder_name = "down";
        public readonly static string default_run_first_after_up_folder_name = "runFirstAfterUp";
        public readonly static string default_functions_folder_name = "functions";
        public readonly static string default_views_folder_name = "views";
        public readonly static string default_sprocs_folder_name = "sprocs";
        public readonly static string default_permissions_folder_name = "permissions";
        public readonly static string default_environment_name = "LOCAL";
        public readonly static string default_roundhouse_schema_name = "RoundhousE";
        public readonly static string default_version_table_name = "Version";
        public readonly static string default_scripts_run_table_name = "ScriptsRun";
        public readonly static string default_scripts_run_errors_table_name = "ScriptsRunErrors";
        public readonly static string default_version_file = @"_BuildInfo.xml";
        public readonly static string default_version_x_path = @"//buildInfo/version";
        public readonly static string default_server_name = "(local)";
        public readonly static string default_output_path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData) + @"\" + name;
        public readonly static string default_database_type = "roundhouse.databases.sqlserver.SqlServerDatabase, roundhouse.databases.sqlserver";
        public readonly static string logging_file = @"C:\Temp\RoundhousE\roundhouse.changes.log";
        public readonly static string log4net_configuration_assembly = @"roundhouse";
        public readonly static string log4net_configuration_resource = @"roundhouse.infrastructure.app.logging.log4net.config.xml";
        public readonly static string log4net_configuration_resource_no_console = @"roundhouse.infrastructure.app.logging.log4net.config.no.console.xml";
        public readonly static int default_command_timeout = 60;
        public readonly static int default_restore_timeout = 900;

        public class CurrentMappings
        {
            public static string roundhouse_schema_name = default_roundhouse_schema_name;
            public static string version_table_name = default_version_table_name;
            public static string scripts_run_table_name = default_scripts_run_table_name;
            public static string scripts_run_errors_table_name = default_scripts_run_errors_table_name;
            public static string database_type = default_database_type;

        }
    }
}