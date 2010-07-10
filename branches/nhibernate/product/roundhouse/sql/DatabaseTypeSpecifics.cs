using System.Collections.Generic;

namespace roundhouse.sql
{
    public static class DatabaseTypeSpecifics
    {
        public static DatabaseTypeSpecific t_sql_specific = new TSQLSpecific();
        public static DatabaseTypeSpecific t_sql2000_specific = new TSQL2000Specific();
        public static DatabaseTypeSpecific access_sql_specific = new AccessSQLSpecific();
        public static DatabaseTypeSpecific pl_sql_specific = new PLSQLSpecific();
        
        public static IDictionary<string, DatabaseTypeSpecific> sql_scripts_dictionary = generate_scripts_dictionary();

        private static IDictionary<string, DatabaseTypeSpecific> generate_scripts_dictionary()
        {
            IDictionary<string, DatabaseTypeSpecific> scripts_dictionary = new Dictionary<string, DatabaseTypeSpecific>();

            scripts_dictionary.Add("SQLServer", t_sql_specific);
            scripts_dictionary.Add("System.Data.SqlClient", t_sql_specific);
            scripts_dictionary.Add("SQLNCLI", t_sql_specific);
            scripts_dictionary.Add("SQLNCLI10", t_sql_specific);
            scripts_dictionary.Add("sqloledb", t_sql_specific);
            scripts_dictionary.Add("Microsoft.SQLSERVER.MOBILE.OLEDB.3.0", t_sql_specific);
            scripts_dictionary.Add("Microsoft.SQLSERVER.CE.OLEDB.3.5", t_sql_specific);
            scripts_dictionary.Add("Microsoft.Jet.OLEDB.4.0", access_sql_specific);
            scripts_dictionary.Add("Microsoft.ACE.OLEDB.12.0", access_sql_specific);
            scripts_dictionary.Add("Oracle", pl_sql_specific);
            scripts_dictionary.Add("SQLServer2000", t_sql2000_specific);
            //scripts_dictionary.Add("MySQLProv", mysql_sql_scripts);
            //scripts_dictionary.Add("MyOracleDB", oracle_sql_scripts);
            //scripts_dictionary.Add("msdaora", oracle_sql_scripts);
            //scripts_dictionary.Add("OraOLEDB.Oracle", oracle_sql_scripts);
            //scripts_dictionary.Add("vfpoledb", vfp_sql_scripts);
            //scripts_dictionary.Add("IBMDADB2", db2_sql_scripts);
            //scripts_dictionary.Add("DB2OLEDB", db2_sql_scripts);
            //scripts_dictionary.Add("PostgreSQL OLE DB Provider", postgre_sql_scripts);

            return scripts_dictionary;
        }


    }
}