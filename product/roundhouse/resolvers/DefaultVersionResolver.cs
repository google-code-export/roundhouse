namespace roundhouse.resolvers
{
    using infrastructure.extensions;
    using infrastructure.filesystem;
    using infrastructure.logging;

    public class DefaultVersionResolver : VersionResolver
    {
        private readonly FileSystemAccess file_system;
        private readonly string x_path;
        private const string dll_extension = ".dll";
        private const string xml_extension = ".xml";

        public DefaultVersionResolver(FileSystemAccess file_system,string x_path)
        {
            this.file_system = file_system;
            this.x_path = x_path;
        }

        public string resolve_version(string version_file)
        {
            string version = "0";
            version_file = file_system.get_full_path(version_file);
            if (version_file_is_xml(version_file))
            {
                Log.bound_to(this).log_an_info_event_containing("Attempting to resolve version from {0} using {1}.", version_file, x_path);
                version = get_version_from_xml(version_file, x_path);
            }
            if (version_file_is_dll(version_file))
            {
                Log.bound_to(this).log_an_info_event_containing("Attempting to resolve assembly file version from {0}.", version_file);
                version = get_version_from_dll(version_file);
            }

            return version;
        }

        private string get_version_from_dll(string version_file)
        {
            return "0";
        }

        private string get_version_from_xml(string version_file, string x_path)
        {
            return "0";
        }

        private bool version_file_is_xml(string version_file)
        {
            return file_system.get_file_extension_from(version_file).to_lower() == xml_extension;
        }

        private bool version_file_is_dll(string version_file)
        {
            return file_system.get_file_extension_from(version_file).to_lower() == dll_extension;
        }
    }
}