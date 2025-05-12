using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using MySqlConnector;

namespace Chronicle.Security.Menu
{
    public partial class Menus : Form
    {
        public Menus()
        {
            InitializeComponent();
            populateTreeGrid();
            checkedListBox1.Items.AddRange(getClasses());
        }


        private void populateTreeGrid()
        {
            treeView1.Nodes.Clear();
            using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
            {
                Queue<MenuTreeItem> open_set = new Queue<MenuTreeItem>();
                conn.Open();
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DISTINCT A.* FROM MENU_ITEMS A, OPERATOR_CLASS B, MENU_ITEM_ACCESS C WHERE A.menuItemID = C.menuItemID AND B.operatorClassID = C.operatorClassID AND A.parentItemID is null ORDER BY sortOrder DESC";


                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    MenuTreeItem itm = new MenuTreeItem(reader["menuItemID"] as int? ?? -1);
                    itm.Text = reader["menuText"] as string ?? "";
                    open_set.Enqueue(itm);
                    treeView1.Nodes.Add(itm);
                }
                reader.Close();
                while (open_set.Count > 0)
                {
                    MenuTreeItem itm = open_set.Dequeue();

                    cmd.Parameters.Clear();
                    cmd.CommandText = "SELECT DISTINCT A.* FROM MENU_ITEMS A, OPERATOR_CLASS B, MENU_ITEM_ACCESS C WHERE A.menuItemID = C.menuItemID AND B.operatorClassID = C.operatorClassID AND A.parentItemID = @pID ORDER BY sortOrder DESC";
                    cmd.Parameters.AddWithValue("@pID", itm.itemID);

                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        MenuTreeItem i = new MenuTreeItem(reader["menuItemID"] as int? ?? -1);
                        i.Text = reader["menuText"] as string ?? "";
                        itm.Nodes.Add(i);
                        open_set.Enqueue(i);
                    }
                    reader.Close();
                }
            }
        }

        public string[] getClasses()
        {
            List<string> classes = new List<string>();
            using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand()
                {
                    Connection = conn,
                    CommandText = "SELECT * FROM OPERATOR_CLASS"
                };
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    classes.Add(reader.GetString("classDescr"));
                }
            }
            return classes.ToArray();
        }

        

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {

        }
    }
}
