using roundhouse.infrastructure.filesystem;

namespace roundhouse.folders
{
    public class DefaultFolder : Folder
    {
        private readonly FileSystemAccess file_system;

        public DefaultFolder(FileSystemAccess file_system, string folder_path, string folder_name,
                             bool should_run_items_in_folder_once)
        {
            this.file_system = file_system;
            this.folder_path = folder_path;
            this.folder_name = folder_name;
            this.should_run_items_in_folder_once = should_run_items_in_folder_once;
        }

        public string folder_name { get; set; }

        public string folder_path { get; private set; }

        public bool should_run_items_in_folder_once { get; private set; }

        public string folder_full_path
        {
            get { return file_system.combine_paths(folder_path, folder_name); }
        }
    }
}