using System;

namespace roundhouse.sql
{
    public class TSQL2000Specific : DatabaseTypeSpecific
    {

        public string create_database(string database_name)
        {
            return string.Format(
                @"USE master 
                        IF NOT EXISTS(SELECT * FROM sysdatabases WHERE [name] = '{0}') 
                         BEGIN 
                            CREATE DATABASE [{0}] 
                         END
                        ",
                database_name);
        }

        public string set_recovery_mode(string database_name, bool simple)
        {
            return string.Format(
                @"USE master 
                   ALTER DATABASE [{0}] SET RECOVERY {1}
                    ",
                database_name, simple ? "SIMPLE" : "FULL");
        }

        public string restore_database(string database_name, string restore_from_path, string custom_restore_options)
        {
            throw new NotImplementedException();
        }

        public string delete_database(string database_name)
        {
            return string.Format(
                @"USE master 
                        IF EXISTS(SELECT * FROM sysdatabases WHERE [name] = '{0}') 
                        BEGIN 
                            ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE                            
                            DROP DATABASE [{0}] 
                        END",
                database_name);
        }

    }
}