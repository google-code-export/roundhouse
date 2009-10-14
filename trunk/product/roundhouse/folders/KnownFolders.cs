namespace roundhouse.folders
{
    public interface KnownFolders
    {
        Folder up { get; }
        Folder down { get; }
        Folder run_first { get; }
        Folder functions { get; }
        Folder views { get; }
        Folder sprocs { get; }
        Folder permissions { get; }
    }
}