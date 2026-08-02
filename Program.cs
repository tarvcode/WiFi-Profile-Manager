using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public class MainForm : Form
    {
        private ListView listView;
        private Button btnRefresh;
        private Button btnDelete;
        private Button btnExit;

        public MainForm()
        {
            Text = "WiFi Profile Manager";
            Width = 500;
            Height = 500;
            StartPosition = FormStartPosition.CenterScreen;

            listView = new ListView
            {
                Left = 10,
                Top = 10,
                Width = 460,
                Height = 380,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            listView.Columns.Add("Profile Name", 440);

            btnRefresh = new Button
            {
                Text = "Refresh",
                Left = 10,
                Top = 400,
                Width = 140,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnRefresh.Click += (s, e) => LoadProfiles();

            btnDelete = new Button
            {
                Text = "Delete Selected",
                Left = 165,
                Top = 400,
                Width = 140,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnDelete.Click += (s, e) => DeleteSelected();

            btnExit = new Button
            {
                Text = "Exit",
                Left = 330,
                Top = 400,
                Width = 140,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnExit.Click += (s, e) => Close();

            Controls.Add(listView);
            Controls.Add(btnRefresh);
            Controls.Add(btnDelete);
            Controls.Add(btnExit);

            Load += (s, e) => LoadProfiles();
        }

        private List<string> GetProfiles()
        {
            var profiles = new List<string>();
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show profiles",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(psi))
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

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
            }
            return profiles;
        }

        private void LoadProfiles()
        {
            listView.Items.Clear();
            var profiles = GetProfiles();
            if (profiles.Count == 0)
            {
                MessageBox.Show("No saved WiFi profiles found.", "WiFi Profile Manager",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (var p in profiles)
            {
                listView.Items.Add(new ListViewItem(p));
            }
        }

        private void DeleteSelected()
        {
            var toDelete = new List<string>();
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Checked)
                    toDelete.Add(item.Text);
            }

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
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan delete profile name=\"{name}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                        errors.AppendLine(name + ": " + output.Trim());
                }
            }

            if (errors.Length > 0)
            {
                MessageBox.Show("Some profiles failed to delete:\n\n" + errors.ToString(),
                    "WiFi Profile Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            LoadProfiles();
        }
    }
}
