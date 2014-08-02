using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhpVersionSwitcher
{
	public partial class MainForm : Form
	{
		private ServiceManager httpServer;
		private VersionsManager phpVersions;

		private ToolStripMenuItem activeVersionItem;
		private ToolStripItem httpServerStart;
		private ToolStripItem httpServerStop;
		private ToolStripItem httpServerRestart;

		private WaitingForm waitingForm;

		public MainForm()
		{
			this.InitializeComponent();
			this.httpServer = new ServiceManager(Properties.Settings.Default.HttpServerServiceName);
			this.phpVersions = new VersionsManager(Properties.Settings.Default.PhpDir, this.httpServer);
			this.waitingForm = new WaitingForm();
			this.InitMainMenu();
		}

		private void InitMainMenu()
		{
			var activeVersion = this.phpVersions.GetActive();
			var versions = this.phpVersions.GetAvailable();

			this.notifyIconMenu.Items.Clear();
			foreach (var version in versions)
			{
				var item = new ToolStripMenuItem(version.ToString(), null, this.version_Clicked);
				item.Tag = version;

				if (activeVersion != null && version.Equals(activeVersion))
				{
					this.SetActiveItem(item);
				}

				this.notifyIconMenu.Items.Add(item);
			}
			this.notifyIconMenu.Items.Add(new ToolStripSeparator());
			this.notifyIconMenu.Items.Add(this.GetHttpServerMenu());
			this.notifyIconMenu.Items.Add("Refresh", null, this.refresh_Clicked);
			this.notifyIconMenu.Items.Add("Close", null, this.close_Click);
		}

		private ToolStripMenuItem GetHttpServerMenu()
		{
			var menu = new ToolStripMenuItem(this.httpServer.ServiceName);
			this.httpServerStart = menu.DropDownItems.Add("Start", null, this.httpServerStart_Clicked);
			this.httpServerStop = menu.DropDownItems.Add("Stop", null, this.httpServerStop_Clicked);
			this.httpServerRestart = menu.DropDownItems.Add("Restart", null, this.httpServerRestart_Clicked);
			this.UpdateHttpServerMenuState();

			return menu;
		}

		private void UpdateHttpServerMenuState()
		{
			bool running = this.httpServer.IsRunning();
			this.httpServerStart.Enabled = !running;
			this.httpServerStop.Enabled = running;
			this.httpServerRestart.Enabled = running;
		}

		private void SetActiveItem(ToolStripMenuItem item)
		{
			if (this.activeVersionItem != null) this.activeVersionItem.Checked = false;
			this.activeVersionItem = item;
			this.activeVersionItem.Checked = true;
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
				catch (ServiceStartFailedException ex)
				{
					var button = MessageBox.Show("Unable to start " + ex.ServiceName + " service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					if (button != DialogResult.Retry) break;
				}
				catch (ServiceStopFailedException ex)
				{
					var button = MessageBox.Show("Unable to stop " + ex.ServiceName + " service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					if (button != DialogResult.Retry) break;
				}
			}

			this.UpdateHttpServerMenuState();
			this.waitingForm.Hide();
			this.notifyIconMenu.Enabled = true;
		}

		private void version_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem) sender;
			var version = (Version) menuItem.Tag;

			this.Attempt("PHP version to change", async () =>
			{
				await this.phpVersions.SwitchTo(version);
				this.SetActiveItem(menuItem);
			});
		}

		private void httpServerStart_Clicked(object sender, EventArgs e)
		{
			this.Attempt("HTTP server to start", this.httpServer.Start);
		}

		private void httpServerStop_Clicked(object sender, EventArgs e)
		{
			this.Attempt("HTTP server to stop", this.httpServer.Stop);
		}

		private void httpServerRestart_Clicked(object sender, EventArgs e)
		{
			this.Attempt("HTTP server to restart", this.httpServer.Restart);
		}

		private void refresh_Clicked(object sender, EventArgs e)
		{
			this.InitMainMenu();
		}

		private void close_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}
