namespace roundhouse.resolvers
{
    public interface VersionResolver
    {
        string resolve_version(string version_file);
    }
}