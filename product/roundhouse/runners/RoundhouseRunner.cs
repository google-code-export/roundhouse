namespace roundhouse.runners
{
    using System.IO;
    using infrastructure.filesystem;
    using infrastructure.logging;
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
        private const string SQL_EXTENSION = "*.sql";

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

            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, up_folder_name), true);

            //todo: remember when looking through all files below here, change CREATE to ALTER
            //todo: we are going to create the create if not exists script

            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, run_first_folder_name), true);
            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, functions_folder_name), true);
            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, views_folder_name), true);
            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, sprocs_folder_name), true);
            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, permissions_folder_name), true);

            //todo: permissions folder is based on environment if there are any environment files

            //todo: version the database
        }

        //todo:down story

        public void traverse_files_and_run_sql(string directory, bool run_once)
        {
            if (!file_system.directory_exists(directory)) return;

            foreach (string sql_file in file_system.get_all_file_name_strings_in(directory, SQL_EXTENSION))
            {
                //todo: add in logic for running only once
                string sql_file_text = File.ReadAllText(sql_file);
                Log.bound_to(this).log_an_info_event_containing("Found and running {0}.", sql_file);
                database.run_sql(server_name, database_name, sql_file_text);
            }

            foreach (string child_directory in file_system.get_all_directory_name_strings_in(directory))
            {
                traverse_files_and_run_sql(child_directory, run_once);
            }
        }

    }
}