using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Chronicle.Security.Menu
{
    public class MenuTreeItem : TreeNode
    {
        public int itemID;

        public MenuTreeItem(int itemID)
        {
            this.itemID = itemID;

        }
    }
}
