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
            clbObjectPerms.Items.AddRange(getClasses());
            nudSortOrder.Maximum = 1000;
            nudSortOrder.Minimum = 0;
            cbClickAction.Items.AddRange(getPluginNames());
            toolStripStatusLabel1.Text = "Ready";
        }


        private string[] getPluginNames()
        {
            List<string> plugins = new List<string>();
            plugins.Add("(none)");
            using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT pluginID FROM PLUGINS";
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    plugins.Add(reader["pluginID"] as string ?? "(null)");
                }
                reader.Close();
            }

            return plugins.ToArray();
        }

        private void populateTreeGrid()
        {
            clbObjectPerms.ItemCheck -= checkedListBox1_ItemCheck;
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
                    itm.Text = reader["menuText"] as string ?? "(null)";
                    itm.parentID = reader["parentItemID"] as int?;
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
                        i.Text = reader["menuText"] as string ?? "(null)";
                        i.parentID = reader["parentItemID"] as int?;
                        itm.Nodes.Add(i);
                        open_set.Enqueue(i);
                    }
                    reader.Close();
                }
            }
            clbObjectPerms.ItemCheck += checkedListBox1_ItemCheck;
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
            if (treeView1.SelectedNode is not MenuTreeItem itm) return;
            if (e.NewValue == CheckState.Checked)
            {
                // Button has been checked. Insert
                using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO MENU_ITEM_ACCESS (menuItemID, operatorClassID, addedBy, addedDt, updateBy, updateDt) VALUES " +
                                      "(@mIID, (SELECT operatorClassID FROM OPERATOR_CLASS WHERE classDescr = @oprClsDescr), @oprID, current_timestamp, @oprID, current_timestamp);";
                    cmd.Parameters.AddWithValue("@mIID", itm.itemID);
                    cmd.Parameters.AddWithValue("@oprClsDescr", e.ToString());
                    cmd.Parameters.AddWithValue("@oprID", Globals.OperatorID);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                // Button has been unchecked. Delete.
                using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM MENU_ITEM_ACCESS WHERE menuItemID = @mIID AND operatorClassID = (SELECT operatorClassID FROM OPERATOR_CLASS WHERE classDescr = @oprClsDescr)";
                    cmd.Parameters.AddWithValue("@mIID", itm.itemID);
                    cmd.Parameters.AddWithValue("@oprClsDescr", e.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node is not MenuTreeItem itm) return;
            clbObjectPerms.ItemCheck -= checkedListBox1_ItemCheck;
            if(itm.itemID == -1)
            {
                txtMenuText.Text = itm.Text;
                nudSortOrder.Value = 0;
                clbObjectPerms.Enabled = false;
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
            {
                conn.Open();

                // Get the data!
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM MENU_ITEMS WHERE menuItemID = @mIID";
                cmd.Parameters.AddWithValue("@mIID", itm.itemID);
                MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read()) return;
                nudSortOrder.Value = reader["sortOrder"] as int? ?? 0;
                txtMenuText.Text = reader["menuText"] as string ?? "(null)";
                cbSubmenuPop.Checked = reader["showInSubmenu"] as bool? ?? false;
                cbClickAction.Text = reader["pluginID"] as string ?? "(null)";
                reader.Close();
                cmd.CommandText = "SELECT B.classDescr FROM MENU_ITEM_ACCESS A, OPERATOR_CLASS B WHERE A.operatorClassID = B.operatorClassID AND A.menuItemID = @mIID";
                List<string> allowedClasses = new List<String>();
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    allowedClasses.Add(reader["classDescr"] as string ?? "");
                }

                for (int i = 0; i < clbObjectPerms.Items.Count; i++)
                {
                    clbObjectPerms.SetItemChecked(i, allowedClasses.Contains(clbObjectPerms.Items[i].ToString() ?? ""));
                }
            }
            clbObjectPerms.ItemCheck += checkedListBox1_ItemCheck;
        }

        private void cbSubmenuPop_CheckedChanged(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode is not MenuTreeItem itm) return;
            using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE MENU_ITEMS SET showInSubmenu=@show WHERE menuItemID = @mIID;";
                cmd.Parameters.AddWithValue("@show", cbSubmenuPop.Checked);
                cmd.Parameters.AddWithValue("@mIID", itm.itemID);
                cmd.ExecuteNonQuery();
            }
        }



        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the drop location.
            Point targetPoint = treeView1.PointToClient(new Point(e.X, e.Y));

            // Retrieve the node at the drop location.
            MenuTreeItem targetNode = treeView1.GetNodeAt(targetPoint) as MenuTreeItem;

            // Retrieve the node that was dragged.
            MenuTreeItem draggedNode = (MenuTreeItem)e.Data.GetData(typeof(MenuTreeItem));

            // Confirm that the node at the drop location is not 
            // the dragged node and that target node isn't null
            // (for example if you drag outside the control)
            if (!draggedNode.Equals(targetNode) && targetNode != null)
            {
                // Remove the node from its current 
                // location and add it to the node at the drop location.
                draggedNode.Remove();
                targetNode.Nodes.Add(draggedNode);
                draggedNode.parentID = targetNode.parentID;

                // Expand the node at the location 
                // to show the dropped node.
                targetNode.Expand();
                return;
            }
            if (targetNode == null)
            {
                // If the target is null, put it at the top level of the tree.
                draggedNode.Remove();
                treeView1.Nodes.Add(draggedNode);
                draggedNode.parentID = null;

                return;
            }
        }

        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Update the selected node
            if (treeView1.SelectedNode is not MenuTreeItem itm) return;

            // Update Name, Action, and Parent
            using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = conn.CreateCommand();
                if(itm.itemID == -1)
                {
                    cmd.CommandText = "INSERT INTO MENU_ITEMS (menuText, parentItemID, showInSubmenu, sortOrder, pluginID)";
                } else {
                    cmd.CommandText = "UPDATE MENU_ITEMS SET menuText = @text, parentItemID = @pIID, sortOrder=@sOrd, pluginID = @pID WHERE menuItemID = @mIID";
                }
                cmd.Parameters.AddWithValue("@text", txtMenuText.Text);
                cmd.Parameters.AddWithValue("@pIID", itm.parentID);
                cmd.Parameters.AddWithValue("@sOrd", nudSortOrder.Value);
                cmd.Parameters.AddWithValue("@pID", cbClickAction.Text);
                int resCount = cmd.ExecuteNonQuery();
                if (resCount != 0)
                {
                    string plural = "";
                    if (resCount != 1)
                    {
                        plural = "s";
                    }
                    toolStripStatusLabel1.Text = $"{resCount} record{plural} updated";
                }
            }

        }

        private void nudSortOrder_Leave(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode is not MenuTreeItem itm) return;
            using (MySqlConnection conn = new MySqlConnection(Globals.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = conn.CreateCommand();
                if (itm.parentID is null)
                {
                    cmd.CommandText = "SELECT 'x' FROM MENU_ITEMS WHERE parentItemID IS NULL AND sortOrder = @sortOrder AND menuItemID <> @mIID;";
                }
                else
                {
                    cmd.CommandText = "SELECT 'x' FROM MENU_ITEMS WHERE parentItemID = @pIID AND sortOrder = @sortOrder AND menuItemID <> @mIID;";
                    cmd.Parameters.AddWithValue("@pIID", itm.parentID);
                }
                cmd.Parameters.AddWithValue("@sortOrder", nudSortOrder.Value);
                cmd.Parameters.AddWithValue("@mIID", itm.itemID);
                if (cmd.ExecuteReader().Read())
                {
                    MessageBox.Show($"Warning! The value {nudSortOrder.Value} is not allowed on this subitem.", "Warning: Unique Data Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MenuTreeItem itm = new MenuTreeItem(-1)
            {
                Text = "New Menu Item",
                parentID = null
            };
            treeView1.Nodes.Add(itm);
        }
    }
}
