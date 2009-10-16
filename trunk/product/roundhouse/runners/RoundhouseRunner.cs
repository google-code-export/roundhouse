using roundhouse.environments;
using roundhouse.folders;

namespace roundhouse.runners
{
    using System.IO;
    using infrastructure.filesystem;
    using infrastructure.logging;
    using migrators;
    using resolvers;

    public class RoundhouseRunner : IRunner
    {
        private readonly string repository_path;
        private readonly Environment environment;
        private readonly KnownFolders known_folders;
        private readonly FileSystemAccess file_system;
        private readonly DatabaseMigrator database_migrator;
        private readonly VersionResolver version_resolver;
        private const string SQL_EXTENSION = "*.sql";

        public RoundhouseRunner(
                string repository_path,
                Environment environment,
                KnownFolders known_folders,
                FileSystemAccess file_system,
                DatabaseMigrator database_migrator,
                VersionResolver version_resolver)
        {
            this.known_folders = known_folders;
            this.repository_path = repository_path;
            this.environment = environment;
            this.file_system = file_system;
            this.database_migrator = database_migrator;
            this.version_resolver = version_resolver;
        }

        public void run()
        {
            //todo: verify by command line first

            string version = version_resolver.resolve_version();
            database_migrator.create_or_restore_database();
            database_migrator.verify_or_create_roundhouse_tables();
            // version the database first (can be backed out later)
            long version_id = database_migrator.version_the_database(repository_path, version);
            
            traverse_files_and_run_sql(known_folders.up.folder_full_path,
                                       known_folders.up.should_run_items_in_folder_once, version_id);
            
            //todo: remember when looking through all files below here, change CREATE to ALTER
            // we are going to create the create if not exists script

            traverse_files_and_run_sql(known_folders.run_first.folder_full_path,
                                       known_folders.run_first.should_run_items_in_folder_once, version_id);
            traverse_files_and_run_sql(known_folders.functions.folder_full_path,
                                       known_folders.functions.should_run_items_in_folder_once, version_id);
            traverse_files_and_run_sql(known_folders.views.folder_full_path,
                                       known_folders.views.should_run_items_in_folder_once, version_id);
            traverse_files_and_run_sql(known_folders.sprocs.folder_full_path,
                                       known_folders.sprocs.should_run_items_in_folder_once, version_id);
            traverse_files_and_run_sql(known_folders.permissions.folder_full_path,
                                       known_folders.permissions.should_run_items_in_folder_once, version_id);
            
            //todo: permissions folder is based on environment if there are any environment files

        }

        //todo: understand what environment you are deploying to so you can decide what to run
        //todo:down story

        public void traverse_files_and_run_sql(string directory, bool run_once, long version_id)
        {
            if (!file_system.directory_exists(directory)) return;

            foreach (string sql_file in file_system.get_all_file_name_strings_in(directory, SQL_EXTENSION))
            {
                string sql_file_text = File.ReadAllText(sql_file);
                Log.bound_to(this).log_a_debug_event_containing("Found and running {0}.", sql_file);
                database_migrator.run_sql(sql_file_text, file_system.get_file_name_from(sql_file), run_once, version_id);
            }

            foreach (string child_directory in file_system.get_all_directory_name_strings_in(directory))
            {
                traverse_files_and_run_sql(child_directory, run_once, version_id);
            }
        }
    }
}