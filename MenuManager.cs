using Chronicle.Plugins.Core;

namespace Chronicle.Security.Menu
{

    public class MenuManager : IPlugable
    {
        public override string PluginName => "Menu Manager";

        public override string PluginDescription => "Allow modification of Menu Security and Actions";

        public override Version Version => new Version(1,0,0,0);

        public override int Execute()
        {
            new Menus().Show();
            return 0;
        }
    }
}
