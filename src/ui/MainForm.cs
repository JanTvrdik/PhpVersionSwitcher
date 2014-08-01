using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhpVersionSwitcher
{
	public partial class MainForm : Form
	{
		private Model model;
		private ToolStripMenuItem activeVersionItem;
		private ToolStripItem httpServerStart;
		private ToolStripItem httpServerStop;
		private ToolStripItem httpServerRestart;
		private WaitingForm waitingForm;

		public MainForm()
		{
			InitializeComponent();
			this.model = new Model(Properties.Settings.Default.PhpDir, Properties.Settings.Default.HttpServerServiceName);
			this.waitingForm = new WaitingForm();
			this.InitMainMenu();
		}

		private void InitMainMenu()
		{
			var activeVersion = this.model.GetActiveVersion();
			var versions = this.model.GetAvailableVersions();

			this.notifyIconMenu.Items.Clear();
			foreach (var version in versions)
			{
				var item = new ToolStripMenuItem(version.ToString(), null, version_Clicked);
				item.Tag = version;

				if (activeVersion != null && version.Equals(activeVersion))
				{
					this.SetActiveItem(item);
				}

				this.notifyIconMenu.Items.Add(item);
			}
			this.notifyIconMenu.Items.Add(new ToolStripSeparator());
			this.notifyIconMenu.Items.Add(this.GetHttpServerMenu());
			this.notifyIconMenu.Items.Add("Refresh", null, refresh_Clicked);
			this.notifyIconMenu.Items.Add("Close", null, close_Click);
		}

		private ToolStripMenuItem GetHttpServerMenu()
		{
			var menu = new ToolStripMenuItem(Properties.Settings.Default.HttpServerServiceName);
			this.httpServerStart = menu.DropDownItems.Add("Start", null, httpServerStart_Clicked);
			this.httpServerStop = menu.DropDownItems.Add("Stop", null, httpServerStop_Clicked);
			this.httpServerRestart = menu.DropDownItems.Add("Restart", null, httpServerRestart_Clicked);
			this.UpdateHttpServerMenuState();

			return menu;
		}

		private void UpdateHttpServerMenuState()
		{
			var running = this.model.IsHttpServerRunning();
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

			try
			{
				await action();
				this.waitingForm.Hide();
			}
			catch (HttpServerStartFailedException)
			{
				this.waitingForm.Hide();
				var serviceName = Properties.Settings.Default.HttpServerServiceName;
				var button = MessageBox.Show("Unable to start " + serviceName + " service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
				if (button == DialogResult.Retry) Attempt(description, action);
			}
			catch (HttpServerStopFailedException)
			{
				this.waitingForm.Hide();
				var serviceName = Properties.Settings.Default.HttpServerServiceName;
				var button = MessageBox.Show("Unable to stop " + serviceName + " service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
				if (button == DialogResult.Retry) Attempt(description, action);
			}
			finally
			{
				this.UpdateHttpServerMenuState();
				this.notifyIconMenu.Enabled = true;
			}
		}

		private void version_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			var version = (Version)menuItem.Tag;

			Attempt("PHP version to change", async () =>
			{
				await this.model.SwitchTo(version);
				this.SetActiveItem(menuItem);
			});
		}

		private void httpServerStart_Clicked(object sender, EventArgs e)
		{
			Attempt("HTTP server to start", async () =>
			{
				await this.model.StartHttpServer();
			});
		}

		private void httpServerStop_Clicked(object sender, EventArgs e)
		{
			Attempt("HTTP server to stop", async () =>
			{
				await this.model.StopHttpServer();
			});
		}

		private void httpServerRestart_Clicked(object sender, EventArgs e)
		{
			Attempt("HTTP server to restart", async () =>
			{
				await this.model.StartHttpServer();
				await this.model.StopHttpServer();
			});
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
