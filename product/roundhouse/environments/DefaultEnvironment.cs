namespace roundhouse.environments
{
    public class DefaultEnvironment : Environment
    {
        public DefaultEnvironment(string name)
        {
            this.name = name;
        }

        public string name { get; private set; }
        
    }
}