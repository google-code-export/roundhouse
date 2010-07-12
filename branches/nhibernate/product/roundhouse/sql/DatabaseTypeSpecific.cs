namespace roundhouse.sql
{
    public interface DatabaseTypeSpecific
    {
        string create_database(string database_name);
        string set_recovery_mode(string database_name, bool simple);
        string restore_database(string database_name, string restore_from_path, string custom_restore_options);
        string delete_database(string database_name);

    }
}