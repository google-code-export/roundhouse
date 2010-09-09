namespace BuildDatabase
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Reflection;
    using NHibernate;
    using NHibernate.Tool.hbm2ddl;
    using Configuration = NHibernate.Cfg.Configuration;

    internal class Program
    {
        private static string ROUNDHOUSE_EXE;
        private static string DB_SERVER;
        private static string DB_NAME;
        private static string PATH_TO_SCRIPTS;
        private static string NAME_OF_SCRIPT;
        private static string PATH_TO_RESTORE;
        private static bool INITIAL_DEVELOPMENT;
        private static string MAPPINGS_ASSEMBLY;
        private static string CONVENTIONS_ASSEMBLY;

        private static void Main(string[] args)
        {
            try
            {
                ROUNDHOUSE_EXE = ConfigurationManager.AppSettings["roundhouse_exe"];
                DB_SERVER = ConfigurationManager.AppSettings["db_server"];
                DB_NAME = ConfigurationManager.AppSettings["db_name"];
                PATH_TO_SCRIPTS = ConfigurationManager.AppSettings["path_to_scripts"];
                NAME_OF_SCRIPT = ConfigurationManager.AppSettings["name_of_script"];
                PATH_TO_RESTORE = ConfigurationManager.AppSettings["path_to_restore"];
                INITIAL_DEVELOPMENT = ConfigurationManager.AppSettings["has_this_installed_into_prod"] == "false";
                MAPPINGS_ASSEMBLY = ConfigurationManager.AppSettings["mapping_assembly_name"];
                CONVENTIONS_ASSEMBLY = ConfigurationManager.AppSettings["conventions_assembly_name"];

                run_roundhouse_nhibernate();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        private static void run_roundhouse_nhibernate()
        {
            if (INITIAL_DEVELOPMENT)
            {
                run_initial_database_setup();
            }
            else
            {
                run_maintenance_database_setup();
            }
        }

        #region initial database setup

        public static void run_initial_database_setup()
        {
            create_the_database(ROUNDHOUSE_EXE,DB_SERVER, DB_NAME);
            build_database_schema(DB_SERVER, DB_NAME);
            run_roundhouse_drop_create(ROUNDHOUSE_EXE,DB_SERVER, DB_NAME, PATH_TO_SCRIPTS);
        }

        private static void create_the_database(string roundhouse_exe, string server_name, string db_name)
        {
            CommandRunner.run(roundhouse_exe, String.Format("/s={0} /db={1} /f={2} /silent /simple", server_name, db_name, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), true);
        }

        private static void build_database_schema(string db_server, string db_name)
        {
            Assembly mapping_assembly = Assembly.LoadFile(Path.GetFullPath(MAPPINGS_ASSEMBLY));
            Assembly convention_assembly = Assembly.LoadFile(Path.GetFullPath(CONVENTIONS_ASSEMBLY));

            ISessionFactory sf = NHibernateSessionFactory.build_session_factory(db_server, db_name,mapping_assembly,convention_assembly, build_schema);
        }

        private static void build_schema(Configuration cfg)
        {
            SchemaExport s = new SchemaExport(cfg);
            s.SetOutputFile(Path.Combine(PATH_TO_SCRIPTS, Path.Combine("Up", NAME_OF_SCRIPT)));
            s.Create(true, false);
        }

        private static void run_roundhouse_drop_create(string roundhouse_exe, string server_name, string db_name, string path_to_scripts)
        {
            CommandRunner.run(roundhouse_exe, String.Format("/s={0} /db={1} /f={2} /silent /drop", server_name, db_name, path_to_scripts), true);
            CommandRunner.run(roundhouse_exe, String.Format("/s={0} /db={1} /f={2} /silent /simple", server_name, db_name, path_to_scripts), true);
        }


        #endregion

        #region maintenance database setup

        public static void run_maintenance_database_setup()
        {
        }

        #endregion

    }
}



//#Region "After A Release To Production"

//    Private Sub RunMaintenanceDevelopmentDatabaseSetup()
//        RestoreTheDatabase()
//        UpdateDatabaseSchema()
//        RunRoundhouseUpdates()
//    End Sub

//    Private Sub RestoreTheDatabase()
//        Dim psi As New ProcessStartInfo(PATH_TO_ROUNDHOUSE, String.Format(" /db={0} /dt=2005 /f={1} /restore /rfp={2} /ni", DB_NAME, PATH_TO_SCRIPTS, PATH_TO_RESTORE))
//        psi.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly.Location)
//        psi.UseShellExecute = False
//        psi.RedirectStandardOutput = False
//        psi.CreateNoWindow = False

//        Using p As Process = New Process
//            p.StartInfo = psi
//            p.Start()
//            p.WaitForExit()
//        End Using
//    End Sub

//    Private Sub UpdateDatabaseSchema()
//        Dim sf As ISessionFactory = NHibernateSetup.Configure(Of CollateralMap)(SERVER_NAME, DB_NAME, AddressOf UpdateSchema).WithoutAuditColumns().WithoutChangeTracking().BuildSessionFactory
//    End Sub

//    Private Sub UpdateSchema(ByVal cfg As Cfg.Configuration)
//        Dim s As New SchemaUpdate(cfg)
//        Dim sb As New StringBuilder

//        s.Execute(AddressOf sb.Append, False)

//        Dim updateScriptFileName As String = PATH_TO_SCRIPTS + "\Up\" + UPDATE_SCRIPT_NAME
//        If File.Exists(updateScriptFileName) Then File.Delete(updateScriptFileName)

//        File.WriteAllText(updateScriptFileName, sb.ToString)
//    End Sub

//    Private Sub RunRoundhouseUpdates()
//        Dim psi As New ProcessStartInfo(PATH_TO_ROUNDHOUSE, String.Format(" /db={0} /dt=2005 /f={1} /ni", DB_NAME, PATH_TO_SCRIPTS))
//        psi.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly.Location)
//        psi.UseShellExecute = False
//        psi.RedirectStandardOutput = False
//        psi.CreateNoWindow = False
//        Using p As Process = New Process
//            p.StartInfo = psi
//            p.Start()
//            p.WaitForExit()
//        End Using
//    End Sub

//#End Region

//End Module