namespace roundhouse.folders
{
    using infrastructure.filesystem;

    public class DefaultMigrationsFolder : DefaultFolder, MigrationsFolder
    {

        public DefaultMigrationsFolder(FileSystemAccess file_system, string folder_path, string folder_name, bool should_run_items_in_folder_once)
            : base(file_system, folder_path, folder_name)
        {
            this.should_run_items_in_folder_once = should_run_items_in_folder_once;
        }

        public bool should_run_items_in_folder_once { get; private set; }
    }
}