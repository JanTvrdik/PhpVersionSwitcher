using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhpVersionSwitcher.Properties;

namespace PhpVersionSwitcher
{
	public partial class MainForm : Form
	{
		private List<ProcessMenu> processMenus;
		private WaitingForm waitingForm;
		private VersionsManager phpVersions;

		public MainForm()
		{
			this.processMenus = new List<ProcessMenu>();
			this.waitingForm = new WaitingForm();

			IProcessManager server = null;

			try
			{
				if (Settings.Default.HttpServerServiceName.Trim().Length > 0)
				{
					server = new ServiceManager(Settings.Default.HttpServerServiceName);
					this.RegisterProcessManager(server);
				}

				if (Settings.Default.HttpServerProcessPath.Trim().Length > 0)
				{
					server = new ProcessManager(Settings.Default.HttpServerProcessPath);
					this.RegisterProcessManager(server);
				}
			
				if (Settings.Default.FastCgiAddress.Trim().Length > 0)
				{
					server = new ProcessManager(Settings.Default.PhpDir + "\\active\\php-cgi.exe", "-b " + Settings.Default.FastCgiAddress);
					this.RegisterProcessManager(server);
				}
			}
			catch (Exception ex)
			{
				this.ShowFatalError("Something went wrong!\n" + ex.Message);
			}

			if (server == null)
			{
				this.ShowFatalError("At least one server must be set");
			}

			this.phpVersions = new VersionsManager(Settings.Default.PhpDir, server);

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
				var item = new ToolStripMenuItem(version);
				item.Checked = version.Equals(activeVersion);
				item.Click += (sender, args) => this.Attempt("PHP version to change", async () =>
				{
					await this.phpVersions.SwitchTo(version);
				});

				this.notifyIconMenu.Items.Add(item);
			}

			this.notifyIconMenu.Items.Add(new ToolStripSeparator());

			foreach (var menu in this.processMenus)
			{
				menu.Refresh();
				this.notifyIconMenu.Items.Add(menu);
			}

			this.notifyIconMenu.Items.Add("Close", null, (sender, args) => Application.Exit());
		}

		private void RegisterProcessManager(IProcessManager pm)
		{
			var menu = new ProcessMenu(pm);
			menu.StartItem.Click += (sender, args) => this.Attempt(pm.Name + " to start", pm.Start);
			menu.StopItem.Click += (sender, args) => this.Attempt(pm.Name + " to stop", pm.Stop);
			menu.RestartItem.Click += (sender, args) => this.Attempt(pm.Name + " to restart", pm.Restart);

			this.processMenus.Add(menu);
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

		private void ShowFatalError(string message)
		{
			MessageBox.Show(message, "Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			Application.Exit();
		}
	}
}
