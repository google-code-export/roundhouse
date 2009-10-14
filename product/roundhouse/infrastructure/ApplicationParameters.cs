namespace roundhouse.infrastructure
{
    public static class ApplicationParameters
    {
        public static string name = "RoundhousE";
        // defaults
        public static string default_up_folder_name = "up";
        public static string default_down_folder_name = "down";
        public static string default_run_first_folder_name = "runFirst";
        public static string default_functions_folder_name = "functions";
        public static string default_views_folder_name = "views";
        public static string default_sprocs_folder_name = "sprocs";
        public static string default_permissions_folder_name = "permissions";
        public static string default_roundhouse_schema_name = "RoundhousE";
        public static string default_version_table_name = "_Version";
        public static string default_scripts_run_table_name = "_ScriptsRun";
        public static string default_version_file = @"..\_BuildInfo.xml";
        public static string default_version_x_path = @"/buildInfo/version";
    }
}