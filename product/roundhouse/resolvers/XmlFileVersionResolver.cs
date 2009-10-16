using roundhouse.infrastructure.extensions;
using roundhouse.infrastructure.filesystem;
using roundhouse.infrastructure.logging;

namespace roundhouse.resolvers
{
    public class XmlFileVersionResolver : VersionResolver
    {
        private readonly FileSystemAccess file_system;
        private readonly string x_path;
        private readonly string version_file;
        private const string xml_extension = ".xml";
      
        public XmlFileVersionResolver(FileSystemAccess file_system, string x_path, string version_file)
        {
            this.file_system = file_system;
            this.x_path = x_path;
            this.version_file = file_system.get_full_path(version_file);
        }

        public bool meets_criteria()
        {
            if (version_file_is_xml(version_file))
            {
                return true;
            }

            return false;
        }

        public string resolve_version()
        {
            Log.bound_to(this).log_an_info_event_containing("Attempting to resolve version from {0} using {1}.",
                                                                version_file, x_path);

            return "0";
        }

        private bool version_file_is_xml(string version_file)
        {
            return file_system.get_file_extension_from(version_file).to_lower() == xml_extension;
        }


    }
}