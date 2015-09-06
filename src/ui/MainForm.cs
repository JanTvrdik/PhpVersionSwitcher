using System;
using System.Collections.Generic;
using System.Linq;
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
			this.notifyIconMenu.Items.AddRange(this.CreateVersionsItems());
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
				var menu = new ProcessMenuGroup(pair.Key, pair.Value);
				var startTasks   = new Func<Task>[pair.Value.Count];
				var stopTasks    = new Func<Task>[pair.Value.Count];
				var restartTasks = new Func<Task>[pair.Value.Count];

				var i = 0;
				foreach (var processMenu in pair.Value)
				{
					startTasks[i]   = processMenu.ProcessManager.Start;
					stopTasks[i]    = processMenu.ProcessManager.Stop;
					restartTasks[i] = processMenu.ProcessManager.Restart;
					i += 1;
				}

				menu.StartItem.Click += (sender, args) => this.Attempt(pair.Key, this.createMultiTask(startTasks));
				menu.StopItem.Click += (sender, args) => this.Attempt(pair.Key, this.createMultiTask(stopTasks));
				menu.RestartItem.Click += (sender, args) => this.Attempt(pair.Key, this.createMultiTask(restartTasks));
				this.notifyIconMenu.Items.Add(menu);
			}

			this.notifyIconMenu.Items.Add(new ToolStripSeparator());
			this.notifyIconMenu.Items.Add("Refresh", null, (sender, args) => this.InitializeMainMenu());
			this.notifyIconMenu.Items.Add("Close", null, (sender, args) => Application.Exit());
			this.notifyIcon.Icon = running ? Resources.Icon_started :  Resources.Icon_stopped;
		}

		private ToolStripMenuItem[] CreateVersionsItems()
		{
			var activeVersion = this.phpVersions.GetActive();
			var versions = this.phpVersions.GetAvailable();

			var groups = versions.GroupBy(version => version.Full);
			var items = groups.Select(group =>
			{
				var children = group.Select(version => CreateVersionItem(version, activeVersion)).ToArray();

				if (children.Length == 1)
				{
					return children.First();
				}
				else
				{
					var item = new ToolStripMenuItem(group.Key);
					item.DropDownItems.AddRange(children);
					return item;
				}
			});

			return items.ToArray();
		}

		private ToolStripMenuItem CreateVersionItem(Version version, Version activeVersion)
		{
			var item = new ToolStripMenuItem(version.Label);
			item.Checked = version.Equals(activeVersion);
			item.Click += (sender, args) => this.Attempt("PHP version to change", async () =>
			{
				await this.phpVersions.SwitchTo(version);
			});

			return item;
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

		private Func<Task> createMultiTask(Func<Task>[] taskRunners)
		{
			return () =>
			{
				var tasks = taskRunners.Select(fn => fn());
				return Task.WhenAll(tasks);
			};
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
