namespace roundhouse.environments
{
    public sealed class DefaultEnvironment : Environment
    {
        public DefaultEnvironment(string name)
        {
            this.name = name;
        }

        public string name { get; private set; }
        
    }
}