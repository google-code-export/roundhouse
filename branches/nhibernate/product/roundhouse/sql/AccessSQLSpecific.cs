using System;

namespace roundhouse.sql
{
    public class AccessSQLSpecific : DatabaseTypeSpecific
    {
        public string create_database(string database_name)
        {
            throw new NotSupportedException("Access does not have a facility for creating a database.");
        }

        public string set_recovery_mode(string database_name, bool simple)
        {
            throw new NotSupportedException("Access does not have a recovery mode.");
        }

        public string restore_database(string database_name, string restore_from_path, string custom_restore_options)
        {
            throw new NotSupportedException("Access does not have a facility for restoring a database.");
        }

        public string delete_database(string database_name)
        {
            throw new NotSupportedException("Access does not have a facility for removing a database.");
        }
    
    }
}