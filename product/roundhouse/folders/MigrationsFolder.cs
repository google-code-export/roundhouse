namespace roundhouse.folders
{
    public interface MigrationsFolder : Folder
    {
        bool should_run_items_in_folder_once { get; }
    }
}