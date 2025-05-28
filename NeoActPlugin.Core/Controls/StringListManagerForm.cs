using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
namespace NeoActPlugin.Core
{
    public class StringListManagerForm : Form
    {
        private ListBox listBox;
        private TextBox inputBox;
        private Button addButton, deleteButton, saveButton;
        private List<string> BossList;
        private string filePath;

        public StringListManagerForm(List<string> stringList, string filePath)
        {
            this.BossList = stringList;
            this.filePath = filePath;

            this.Text = "BOSS清單管理器";
            this.Width = 450;
            this.Height = 300;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;

            listBox = new ListBox { Dock = DockStyle.Top, Height = 200 };

            inputBox = new TextBox
            {
                Dock = DockStyle.Top
            };

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40 };
            addButton = new Button { Text = "新增" };
            deleteButton = new Button { Text = "刪除" };
            saveButton = new Button { Text = "儲存" };

            addButton.Click += AddButton_Click;
            deleteButton.Click += DeleteButton_Click;
            saveButton.Click += SaveButton_Click;

            buttonPanel.Controls.AddRange(new Control[] { addButton, deleteButton, saveButton });

            this.Controls.Add(buttonPanel);
            this.Controls.Add(inputBox);
            this.Controls.Add(listBox);

            RefreshListBox();
        }

        public bool IsBoss(string target)
        {
            foreach (var name in BossList)
            {
                if (target.Contains(name))
                {
                    return true;
                }
            }
            return false;
        }
        private void AddButton_Click(object sender, EventArgs e)
        {
            var text = inputBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                BossList.Add(text);
                inputBox.Clear();
                RefreshListBox();
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (listBox.SelectedIndex >= 0)
            {
                BossList.RemoveAt(listBox.SelectedIndex);
                RefreshListBox();
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllLines(filePath, BossList);
                this.Hide(); // 隱藏視窗
            }
            catch (Exception ex)
            {
                MessageBox.Show("儲存失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshListBox()
        {
            listBox.Items.Clear();
            listBox.Items.AddRange(BossList.ToArray());
        }
    }
}
