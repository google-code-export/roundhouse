namespace roundhouse.runners
{
    using System.IO;
    using infrastructure.filesystem;
    using infrastructure.logging;
    using migrators;

    public class RoundhouseRunner : IRunner
    {
        private readonly string repository_path;
        private readonly string repository_version;
        private readonly string sql_files_directory;
        private readonly string up_folder_name;
        private readonly string down_folder_name;
        private readonly string run_first_folder_name;
        private readonly string functions_folder_name;
        private readonly string views_folder_name;
        private readonly string sprocs_folder_name;
        private readonly string permissions_folder_name;
        private readonly FileSystemAccess file_system;
        private readonly DatabaseMigrator database_migrator;
        private const string SQL_EXTENSION = "*.sql";

        public RoundhouseRunner(
                string repository_path,
                string repository_version,
                string sql_files_directory,
                string up_folder_name,
                string down_folder_name,
                string run_first_folder_name,
                string functions_folder_name,
                string views_folder_name,
                string sprocs_folder_name,
                string permissions_folder_name,
                FileSystemAccess file_system,
                DatabaseMigrator database_migrator)
        {
            this.repository_path = repository_path;
            this.repository_version = repository_version;
            this.sql_files_directory = sql_files_directory;
            this.up_folder_name = up_folder_name;
            this.down_folder_name = down_folder_name;
            this.run_first_folder_name = run_first_folder_name;
            this.functions_folder_name = functions_folder_name;
            this.views_folder_name = views_folder_name;
            this.sprocs_folder_name = sprocs_folder_name;
            this.permissions_folder_name = permissions_folder_name;
            this.file_system = file_system;
            this.database_migrator = database_migrator;
        }

        public void run()
        {
            database_migrator.create_database();
            database_migrator.verify_or_create_roundhouse_tables();
            // version the database first (can be backed out later)
            database_migrator.version_the_database(repository_path, repository_version);

            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, up_folder_name), true);

            //todo: remember when looking through all files below here, change CREATE to ALTER
            // we are going to create the create if not exists script

            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, run_first_folder_name), true);
            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, functions_folder_name), false);
            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, views_folder_name), false);
            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, sprocs_folder_name), false);
            traverse_files_and_run_sql(file_system.combine_paths(sql_files_directory, permissions_folder_name), false);

            //todo: permissions folder is based on environment if there are any environment files

        }

        //todo:down story

        public void traverse_files_and_run_sql(string directory, bool run_once)
        {
            if (!file_system.directory_exists(directory)) return;

            foreach (string sql_file in file_system.get_all_file_name_strings_in(directory, SQL_EXTENSION))
            {
                string sql_file_text = File.ReadAllText(sql_file);
                Log.bound_to(this).log_a_debug_event_containing("Found and running {0}.", sql_file);
                database_migrator.run_sql(sql_file_text, file_system.get_file_name_from(sql_file), run_once);
            }

            foreach (string child_directory in file_system.get_all_directory_name_strings_in(directory))
            {
                traverse_files_and_run_sql(child_directory, run_once);
            }
        }
    }
}