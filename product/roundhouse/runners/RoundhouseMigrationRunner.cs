using roundhouse.folders;

namespace roundhouse.runners
{
    using System;
    using System.IO;
    using infrastructure;
    using infrastructure.filesystem;
    using infrastructure.logging;
    using migrators;
    using resolvers;
    using Environment = roundhouse.environments.Environment;

    public sealed class RoundhouseMigrationRunner : IRunner
    {
        private readonly string repository_path;
        private readonly Environment environment;
        private readonly KnownFolders known_folders;
        private readonly FileSystemAccess file_system;
        private readonly DatabaseMigrator database_migrator;
        private readonly VersionResolver version_resolver;
        private readonly bool interactive;
        private readonly bool dropping_the_database;
        private const string SQL_EXTENSION = "*.sql";

        public RoundhouseMigrationRunner(
                string repository_path,
                Environment environment,
                KnownFolders known_folders,
                FileSystemAccess file_system,
                DatabaseMigrator database_migrator,
                VersionResolver version_resolver,
                bool interactive,
                bool dropping_the_database)
        {
            this.known_folders = known_folders;
            this.repository_path = repository_path;
            this.environment = environment;
            this.file_system = file_system;
            this.database_migrator = database_migrator;
            this.version_resolver = version_resolver;
            this.interactive = interactive;
            this.dropping_the_database = dropping_the_database;
        }

        public void run()
        {

            Log.bound_to(this).log_an_info_event_containing("Running {0} against {1} - {2}. Looking in {3} for scripts to run.",
                    ApplicationParameters.name,
                    database_migrator.database.server_name,
                    database_migrator.database.database_name,
                    known_folders.up.folder_path);
            if (interactive)
            {
                Log.bound_to(this).log_an_info_event_containing("Please press enter when ready to kick...");
                Console.ReadLine();
            }



            create_change_drop_folder();
            Log.bound_to(this).log_a_debug_event_containing("The change_drop folder is: {0}", known_folders.change_drop.folder_full_path);

            create_share_and_set_permissions_for_change_drop_folder();

            database_migrator.backup_database_if_it_exists();

            if (!dropping_the_database)
            {

                database_migrator.create_or_restore_database();
                database_migrator.verify_or_create_roundhouse_tables();

                string current_version = database_migrator.get_current_version(repository_path);
                string new_version = version_resolver.resolve_version();
                Log.bound_to(this).log_an_info_event_containing("Migrating {0} from version {1} to {2}.", database_migrator.database.database_name, current_version, new_version);


                long version_id = database_migrator.version_the_database(repository_path, new_version);

                traverse_files_and_run_sql(known_folders.up.folder_full_path, version_id, known_folders.up);

                //todo: remember when looking through all files below here, change CREATE to ALTER
                // we are going to create the create if not exists script

                traverse_files_and_run_sql(known_folders.run_first_after_up.folder_full_path, version_id, known_folders.run_first_after_up);
                traverse_files_and_run_sql(known_folders.functions.folder_full_path, version_id, known_folders.functions);
                traverse_files_and_run_sql(known_folders.views.folder_full_path, version_id, known_folders.views);
                traverse_files_and_run_sql(known_folders.sprocs.folder_full_path, version_id, known_folders.sprocs);
                traverse_files_and_run_sql(known_folders.permissions.folder_full_path, version_id, known_folders.permissions);

                //todo: permissions folder is based on environment if there are any environment files

                remove_share_from_change_drop_folder();
                Log.bound_to(this).log_an_info_event_containing("{0}{0}{1} has kicked your database ({2})! You are now at version {3}. All changes and backups can be found at \"{4}\".",
                                            System.Environment.NewLine,
                                            ApplicationParameters.name,
                                            database_migrator.database.database_name,
                                            new_version,
                                            known_folders.change_drop.folder_full_path);
            }
            else
            {
                database_migrator.delete_database();
                Log.bound_to(this).log_an_info_event_containing("{0}{0}{1} has removed database ({2}). All changes and backups can be found at \"{3}\".",
                                            System.Environment.NewLine,
                                            ApplicationParameters.name,
                                            database_migrator.database.database_name,
                                            known_folders.change_drop.folder_full_path);
            }
        }

        //todo: write a file_log to the change_drop folder

        private void create_change_drop_folder()
        {
            file_system.create_directory(known_folders.change_drop.folder_full_path);
        }

        private void create_share_and_set_permissions_for_change_drop_folder()
        {
            //todo: implement creating share with change permissions
            //todo: implement setting Everyone to full acess to this folder
        }

        private void remove_share_from_change_drop_folder()
        {
            //todo: implement removal of the file share
        }


        //todo: understand what environment you are deploying to so you can decide what to run
        //todo:down story

        public void traverse_files_and_run_sql(string directory, long version_id, MigrationsFolder migration_folder)
        {
            if (!file_system.directory_exists(directory)) return;

            foreach (string sql_file in file_system.get_all_file_name_strings_in(directory, SQL_EXTENSION))
            {
                string sql_file_text = File.ReadAllText(sql_file);
                Log.bound_to(this).log_a_debug_event_containing("Found and running {0}.", sql_file);
                bool the_sql_ran = database_migrator.run_sql(sql_file_text, file_system.get_file_name_from(sql_file), migration_folder.should_run_items_in_folder_once, version_id);
                if (the_sql_ran)
                {
                    copy_to_change_drop_folder(sql_file, migration_folder);
                }
            }

            foreach (string child_directory in file_system.get_all_directory_name_strings_in(directory))
            {
                traverse_files_and_run_sql(child_directory, version_id, migration_folder);
            }
        }

        private void copy_to_change_drop_folder(string sql_file_ran, Folder migration_folder)
        {
            string destination_file = file_system.combine_paths(known_folders.change_drop.folder_full_path, "itemsRan", sql_file_ran.Replace(migration_folder.folder_path + "\\", string.Empty));
            file_system.verify_or_create_directory(file_system.get_directory_name_from(destination_file));
            Log.bound_to(this).log_a_debug_event_containing("Copying file {0} to {1}.", file_system.get_file_name_from(sql_file_ran), destination_file);
            file_system.file_copy(sql_file_ran, destination_file, true);
        }
    }
}