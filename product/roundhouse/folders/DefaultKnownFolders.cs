namespace roundhouse.folders
{
    public class DefaultKnownFolders : KnownFolders
    {
        public DefaultKnownFolders(Folder up,
                                   Folder down,
                                   Folder run_first,
                                   Folder functions,
                                   Folder views,
                                   Folder sprocs,
                                   Folder permissions
            )
        {
            this.up = up;
            this.down = down;
            this.run_first = run_first;
            this.functions = functions;
            this.views = views;
            this.sprocs = sprocs;
            this.permissions = permissions;
        }

        public Folder up {get;private set;}
        
        public Folder down {get;private set;}
       
        public Folder run_first {get;private set;}
        
        public Folder functions {get;private set;}
      
        public Folder views {get;private set;}
      
        public Folder sprocs {get;private set;}
       
        public Folder permissions {get;private set;}
    }
}