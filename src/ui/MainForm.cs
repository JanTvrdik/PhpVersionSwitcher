using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhpVersionSwitcher.Properties;

namespace PhpVersionSwitcher
{
	internal partial class MainForm : Form
	{
		private IList<IProcessManager> processManagers;
		private VersionsManager phpVersions;
		private WaitingForm waitingForm;

		public MainForm(IList<IProcessManager> processManagers, VersionsManager phpVersions, WaitingForm waitingForm)
		{
			this.processManagers = processManagers;
			this.phpVersions = phpVersions;
			this.waitingForm = waitingForm;

			this.InitializeComponent();
			this.InitializeMainMenu();
		}

		private void InitializeMainMenu()
		{
			this.notifyIconMenu.Items.Clear();
			var activeVersion = this.phpVersions.GetActive();
			var versions = this.phpVersions.GetAvailable();

			foreach (var version in versions)
			{
				var item = new ToolStripMenuItem(version.Label);
				item.Checked = version.Equals(activeVersion);
				item.Click += (sender, args) => this.Attempt("PHP version to change", async () =>
				{
					await this.phpVersions.SwitchTo(version);
				});

				this.notifyIconMenu.Items.Add(item);
			}

			this.notifyIconMenu.Items.Add(new ToolStripSeparator());
			var menuGroups = new Dictionary<string, List<ProcessMenu> >();

			var running = false;
			foreach (var pm in this.processManagers)
			{
				var menu = new ProcessMenu(pm);
				menu.StartItem.Click += (sender, args) => this.Attempt(pm.Name + " to start", pm.Start);
				menu.StopItem.Click += (sender, args) => this.Attempt(pm.Name + " to stop", pm.Stop);
				menu.RestartItem.Click += (sender, args) => this.Attempt(pm.Name + " to restart", pm.Restart);
				if (pm.IsRunning())
				{
					running = true;
				}

				if (pm.GroupName != null)
				{
					if (!menuGroups.ContainsKey(pm.GroupName)) menuGroups.Add(pm.GroupName, new List<ProcessMenu>());
					menuGroups[pm.GroupName].Add(menu);
				}
				else
				{
					this.notifyIconMenu.Items.Add(menu);
				}
			}

			foreach (var pair in menuGroups)
			{
				this.notifyIconMenu.Items.Add(new ProcessMenuGroup(pair.Key, pair.Value));
			}

			this.notifyIconMenu.Items.Add(new ToolStripSeparator());
			this.notifyIconMenu.Items.Add("Refresh", null, (sender, args) => this.InitializeMainMenu());
			this.notifyIconMenu.Items.Add("Close", null, (sender, args) => Application.Exit());
			this.notifyIcon.Icon = running ? Resources.Icon_started :  Resources.Icon_stopped;
		}

		private async void Attempt(string description, Func<Task> action)
		{
			this.notifyIconMenu.Enabled = false;
			this.waitingForm.description.Text = @"Waiting for " + description + @"...";
			this.waitingForm.Show();

			while (true)
			{
				try
				{
					await action();
					break;
				}
				catch (ProcessException ex)
				{
					var msg = "Unable to " + ex.Operation + " " + ex.Name + ".";
					var dialogResult = MessageBox.Show(msg, "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					if (dialogResult != DialogResult.Retry) break;
				}
			}
			
			this.InitializeMainMenu();
			this.waitingForm.Hide();
			this.notifyIconMenu.Enabled = true;
		}

		private void notifyIcon_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				// reflection hack, see http://stackoverflow.com/questions/2208690/invoke-notifyicons-context-menu
				MethodInfo method = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
				method.Invoke(sender, null);
			}
		}
	}
}
