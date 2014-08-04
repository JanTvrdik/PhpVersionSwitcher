using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhpVersionSwitcher.Properties;

namespace PhpVersionSwitcher
{
	public partial class MainForm : Form
	{
		private IList<IProcessManager> processManagers;
		private VersionsManager phpVersions;
		private WaitingForm waitingForm;

		public MainForm()
		{
			try
			{
				this.processManagers = new List<IProcessManager>();
				this.phpVersions = new VersionsManager(Settings.Default.PhpDir, this.processManagers);
				this.waitingForm = new WaitingForm();

				if (Settings.Default.HttpServerServiceName.Trim().Length > 0)
				{
					this.processManagers.Add(new ServiceManager(Settings.Default.HttpServerServiceName));
				}

				if (Settings.Default.HttpServerProcessPath.Trim().Length > 0)
				{
					this.processManagers.Add(new ProcessManager(Settings.Default.HttpServerProcessPath));
				}
			
				if (Settings.Default.FastCgiAddress.Trim().Length > 0)
				{
					this.processManagers.Add(new ProcessManager(Settings.Default.PhpDir + "\\active\\php-cgi.exe", "-b " + Settings.Default.FastCgiAddress));
				}

				this.InitializeComponent();
				this.InitializeMainMenu();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Something went wrong!\n" + ex.Message, "Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Application.Exit();
			}
		}

		private void InitializeMainMenu()
		{
			this.notifyIconMenu.Items.Clear();
			var activeVersion = this.phpVersions.GetActive();
			var versions = this.phpVersions.GetAvailable();

			foreach (var version in versions)
			{
				var item = new ToolStripMenuItem(version);
				item.Checked = version.Equals(activeVersion);
				item.Click += (sender, args) => this.Attempt("PHP version to change", async () =>
				{
					await this.phpVersions.SwitchTo(version);
				});

				this.notifyIconMenu.Items.Add(item);
			}

			this.notifyIconMenu.Items.Add(new ToolStripSeparator());

			foreach (var pm in this.processManagers)
			{
				var menu = new ProcessMenu(pm);
				menu.StartItem.Click += (sender, args) => this.Attempt(pm.Name + " to start", pm.Start);
				menu.StopItem.Click += (sender, args) => this.Attempt(pm.Name + " to stop", pm.Stop);
				menu.RestartItem.Click += (sender, args) => this.Attempt(pm.Name + " to restart", pm.Restart);
				this.notifyIconMenu.Items.Add(menu);
			}

			this.notifyIconMenu.Items.Add("Close", null, (sender, args) => Application.Exit());
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
	}
}
