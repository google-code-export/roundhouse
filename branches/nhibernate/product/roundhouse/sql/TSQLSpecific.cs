namespace roundhouse.sql
{
    using infrastructure.extensions;

    public class TSQLSpecific : DatabaseTypeSpecific
    {
        public string create_database(string database_name)
        {
            return string.Format(
                @"USE master 
                        IF NOT EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
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
            string restore_options = string.Empty;
            if (!string.IsNullOrEmpty(custom_restore_options))
            {
                restore_options = custom_restore_options.to_lower().StartsWith(",") ? custom_restore_options : ", " + custom_restore_options;
            }

            return string.Format(
                @"USE master 
                        ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        
                        RESTORE DATABASE [{0}]
                        FROM DISK = N'{1}'
                        WITH NOUNLOAD
                        , STATS = 10
                        , RECOVERY
                        , REPLACE
                        {2};

                        ALTER DATABASE [{0}] SET MULTI_USER;
                        --DBCC SHRINKDATABASE ([{0}]);
                        ",
                database_name, restore_from_path,
                restore_options
                );
        }

        public string delete_database(string database_name)
        {
            return string.Format(
                @"USE master 
                        IF EXISTS(SELECT * FROM sys.databases WHERE [name] = '{0}') 
                        BEGIN 
                            ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
                            EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = '{0}' 
                            DROP DATABASE [{0}] 
                        END",
                database_name);
        }

    }
}