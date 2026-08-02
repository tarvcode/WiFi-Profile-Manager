using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WifiProfileManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class ProfileInfo
    {
        public string Name { get; set; } = "";
        public bool AutoConnect { get; set; }
        public int Priority { get; set; }
    }

    public class MainForm : Form
    {
        private ListView listView;
        private Button btnRefresh;
        private Button btnDelete;
        private Button btnEnableAuto;
        private Button btnDisableAuto;
        private Button btnUp;
        private Button btnDown;
        private Button btnExit;

        private string interfaceName = "Wi-Fi";

        public MainForm()
        {
            Text = "WiFi Profile Manager";
            Width = 620;
            Height = 560;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new System.Drawing.Size(560, 460);

            listView = new ListView
            {
                Left = 10,
                Top = 10,
                Width = 580,
                Height = 400,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                MultiSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            listView.Columns.Add("Profile Name", 340);
            listView.Columns.Add("AutoConnect", 100);
            listView.Columns.Add("Priority", 100);
            listView.SelectedIndexChanged += (s, e) => UpdateButtonStates();

            int row1Y = 420;
            int row2Y = 455;

            btnRefresh = new Button { Text = "Refresh", Left = 10, Top = row1Y, Width = 110 };
            btnRefresh.Click += (s, e) => LoadProfiles();

            btnEnableAuto = new Button { Text = "Enable Auto-Connect", Left = 130, Top = row1Y, Width = 150 };
            btnEnableAuto.Click += (s, e) => SetAutoConnect(true);

            btnDisableAuto = new Button { Text = "Disable Auto-Connect", Left = 290, Top = row1Y, Width = 150 };
            btnDisableAuto.Click += (s, e) => SetAutoConnect(false);

            btnDelete = new Button { Text = "Delete Selected", Left = 450, Top = row1Y, Width = 140 };
            btnDelete.Click += (s, e) => DeleteSelected();

            btnUp = new Button { Text = "Move Up", Left = 10, Top = row2Y, Width = 110 };
            btnUp.Click += (s, e) => MovePriority(-1);

            btnDown = new Button { Text = "Move Down", Left = 130, Top = row2Y, Width = 110 };
            btnDown.Click += (s, e) => MovePriority(1);

            btnExit = new Button { Text = "Exit", Left = 480, Top = row2Y, Width = 110 };
            btnExit.Click += (s, e) => Close();

            foreach (var b in new[] { btnRefresh, btnEnableAuto, btnDisableAuto, btnDelete, btnUp, btnDown, btnExit })
            {
                b.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                Controls.Add(b);
            }
            btnExit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            Controls.Add(listView);

            Load += (s, e) =>
            {
                DetectInterfaceName();
                LoadProfiles();
            };
        }

        private void DetectInterfaceName()
        {
            try
            {
                string output = RunNetsh("wlan show interfaces");
                foreach (var rawLine in output.Split('\n'))
                {
                    string line = rawLine.TrimEnd('\r');
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("Name") && line.Contains(":"))
                    {
                        int idx = line.IndexOf(':');
                        interfaceName = line.Substring(idx + 1).Trim();
                        break;
                    }
                }
            }
            catch
            {
            }
        }

        private string RunNetsh(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var proc = Process.Start(psi))
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return output;
            }
        }

        private List<string> GetProfileNamesInPriorityOrder()
        {
            var profiles = new List<string>();
            string output = RunNetsh("wlan show profiles");

            foreach (var rawLine in output.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                int idx = line.IndexOf(':');
                if (idx < 0) continue;

                string label = line.Substring(0, idx).Trim();
                if (label.EndsWith("All User Profile") || label.EndsWith("Current User Profile"))
                {
                    string name = line.Substring(idx + 1).Trim();
                    if (!string.IsNullOrEmpty(name))
                        profiles.Add(name);
                }
            }
            return profiles;
        }

        private bool GetAutoConnect(string profileName)
        {
            string output = RunNetsh($"wlan show profile name=\"{profileName}\" interface=\"{interfaceName}\"");
            foreach (var rawLine in output.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.TrimStart().StartsWith("Connection mode"))
                {
                    int idx = line.IndexOf(':');
                    if (idx >= 0)
                    {
                        string val = line.Substring(idx + 1).Trim();
                        return val.IndexOf("automatically", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
            }
            return false;
        }

        private List<ProfileInfo> GetAllProfileInfo()
        {
            var names = GetProfileNamesInPriorityOrder();
            var result = new List<ProfileInfo>();
            for (int i = 0; i < names.Count; i++)
            {
                result.Add(new ProfileInfo
                {
                    Name = names[i],
                    Priority = i + 1,
                    AutoConnect = GetAutoConnect(names[i])
                });
            }
            return result;
        }

        private void LoadProfiles()
        {
            listView.Items.Clear();
            var profiles = GetAllProfileInfo();

            if (profiles.Count == 0)
            {
                UpdateButtonStates();
                MessageBox.Show("No saved WiFi profiles found.", "WiFi Profile Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var p in profiles.OrderBy(p => p.Priority))
            {
                var item = new ListViewItem(p.Name);
                item.SubItems.Add(p.AutoConnect ? "Yes" : "No");
                item.SubItems.Add(p.Priority.ToString());
                item.Tag = p;
                listView.Items.Add(item);
            }

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool singleSelected = listView.SelectedItems.Count == 1;
            btnUp.Enabled = singleSelected;
            btnDown.Enabled = singleSelected;
        }

        private List<string> GetCheckedNames()
        {
            var names = new List<string>();
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Checked)
                    names.Add(item.Text);
            }
            return names;
        }

        private void DeleteSelected()
        {
            var toDelete = GetCheckedNames();
            if (toDelete.Count == 0)
            {
                MessageBox.Show("Check one or more profiles to delete.", "WiFi Profile Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Delete the following profile(s)?");
            sb.AppendLine();
            foreach (var name in toDelete) sb.AppendLine(" - " + name);

            var confirm = MessageBox.Show(sb.ToString(), "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            var errors = new StringBuilder();
            foreach (var name in toDelete)
            {
                string output = RunNetsh($"wlan delete profile name=\"{name}\" interface=\"{interfaceName}\"");
                if (output.IndexOf("deleted", StringComparison.OrdinalIgnoreCase) < 0)
                    errors.AppendLine(name + ": " + output.Trim());
            }

            if (errors.Length > 0)
            {
                MessageBox.Show("Some profiles failed to delete:\n\n" + errors.ToString(),
                    "WiFi Profile Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            LoadProfiles();
        }

        private void SetAutoConnect(bool enable)
        {
            var targets = GetCheckedNames();
            if (targets.Count == 0)
            {
                MessageBox.Show("Check one or more profiles first.", "WiFi Profile Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string mode = enable ? "auto" : "manual";
            var errors = new StringBuilder();
            foreach (var name in targets)
            {
                string output = RunNetsh($"wlan set profileparameter name=\"{name}\" interface=\"{interfaceName}\" ConnectionMode={mode}");
                if (output.IndexOf("updated", StringComparison.OrdinalIgnoreCase) < 0
                    && output.IndexOf("Ok", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    errors.AppendLine(name + ": " + output.Trim());
                }
            }

            if (errors.Length > 0)
            {
                MessageBox.Show("Some profiles failed to update (try running as Administrator):\n\n" + errors.ToString(),
                    "WiFi Profile Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            LoadProfiles();
        }

        private void MovePriority(int direction)
        {
            if (listView.SelectedItems.Count != 1) return;

            var info = (ProfileInfo)listView.SelectedItems[0].Tag;
            int newPriority = info.Priority + direction;
            int maxPriority = listView.Items.Count;
            if (newPriority < 1 || newPriority > maxPriority) return;

            string output = RunNetsh($"wlan set profileorder name=\"{info.Name}\" interface=\"{interfaceName}\" priority={newPriority}");
            if (output.IndexOf("updated", StringComparison.OrdinalIgnoreCase) < 0
                && output.IndexOf("Ok", StringComparison.OrdinalIgnoreCase) < 0)
            {
                MessageBox.Show("Failed to change priority (try running as Administrator):\n\n" + output,
                    "WiFi Profile Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            LoadProfiles();

            foreach (ListViewItem item in listView.Items)
            {
                if (item.Text == info.Name)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }
    }
}
