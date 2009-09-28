namespace roundhouse.runners
{
    using infrastructure.filesystem;
    using sql;

    public class RoundhouseRunner : IRunner
    {
        private readonly string server_name;
        private readonly string database_name;
        private readonly string repository_url;
        private readonly string sql_files_directory;
        private readonly string up_folder_name;
        private readonly string down_folder_name;
        private readonly string run_first_folder_name;
        private readonly string functions_folder_name;
        private readonly string views_folder_name;
        private readonly string sprocs_folder_name;
        private readonly string permissions_folder_name;
        private readonly string version_table_name;
        private readonly FileSystemAccess file_system;
        private readonly Database database;

        public RoundhouseRunner(string server_name, 
                string database_name, 
                string repository_url, 
                string sql_files_directory, 
                string up_folder_name,
                string down_folder_name, 
                string run_first_folder_name, 
                string functions_folder_name, 
                string views_folder_name,
                string sprocs_folder_name, 
                string permissions_folder_name, 
                string version_table_name,
                FileSystemAccess file_system,
                Database database)
        {
            this.server_name = server_name;
            this.database_name = database_name;
            this.repository_url = repository_url;
            this.sql_files_directory = sql_files_directory;
            this.up_folder_name = up_folder_name;
            this.down_folder_name = down_folder_name;
            this.run_first_folder_name = run_first_folder_name;
            this.functions_folder_name = functions_folder_name;
            this.views_folder_name = views_folder_name;
            this.sprocs_folder_name = sprocs_folder_name;
            this.permissions_folder_name = permissions_folder_name;
            this.version_table_name = version_table_name;
            this.file_system = file_system;
            this.database = database;
        }

        public void run()
        {
            database.create_database(server_name, database_name);

            //FolderTraverser folder_traverser = new FolderTraverser();

            //todo: run up folder and all subdirectories (subdirectories first? NO)

            //todo: remember when looking through all files below here, change CREATE to ALTER
            //todo: we are going to create the create if not exists script

            //todo: run special run first folder and all subdirectories

            //todo: run functions folder and all subdirectories

            //todo: run views folder and all subdirectories

            //todo: run sprocs folder and all subdirectories

            //todo: run permissions folder and all subdirectories
            // permissions folder is based on environment if there are any environment files
            
            //todo: version the database
        }
    }
}