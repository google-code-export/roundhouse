namespace roundhouse.folders
{
    public interface Folder
    {
        string folder_name { get; set; }
        string folder_path { get; }
        bool should_run_items_in_folder_once { get; }
        string folder_full_path { get; }
    }
}