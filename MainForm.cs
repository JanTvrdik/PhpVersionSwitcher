using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhpVersionSwitcher
{
	public partial class MainForm : Form
	{
		private Model model;
		private ToolStripMenuItem activeVersion;
		private ToolStripItem httpServerStart;
		private ToolStripItem httpServerStop;
		private ToolStripItem httpServerRestart;
		private WaitingForm waitingForm;

		public MainForm()
		{
			InitializeComponent();
			this.notifyIcon.Icon = this.Icon;
			this.model = new Model(Properties.Settings.Default.PhpDir, Properties.Settings.Default.HttpServerServiceName);
			this.waitingForm = new WaitingForm();
			this.initMainMenu();
		}

		private void initMainMenu()
		{
			var activeVersion = this.model.ActiveVersion;
			var versions = this.model.AvailableVersions;

			this.notifyIconMenu.Items.Clear();
			foreach (var version in versions)
			{
				var item = new ToolStripMenuItem(version.ToString(), null, new EventHandler(version_Clicked));
				item.Tag = version;

				if (activeVersion != null && version.Equals(activeVersion))
				{
					this.setActiveItem(item);
				}

				this.notifyIconMenu.Items.Add(item);
			}
			this.notifyIconMenu.Items.Add(new ToolStripSeparator());
			this.notifyIconMenu.Items.Add(this.getHttpServerMenu());
			this.notifyIconMenu.Items.Add("Refresh", null, new EventHandler(refresh_Clicked));
			this.notifyIconMenu.Items.Add("Close", null, new EventHandler(close_Click));
		}

		private ToolStripMenuItem getHttpServerMenu()
		{
			var menu = new ToolStripMenuItem(Properties.Settings.Default.HttpServerServiceName);
			this.httpServerStart = menu.DropDownItems.Add("Start", null, new EventHandler(httpServerStart_Clicked));
			this.httpServerStop = menu.DropDownItems.Add("Stop", null, new EventHandler(httpServerStop_Clicked));
			this.httpServerRestart = menu.DropDownItems.Add("Restart", null, new EventHandler(httpServerRestart_Clicked));
			this.updateHttpServerMenuState();

			return menu;
		}

		private void updateHttpServerMenuState()
		{
			var running = this.model.IsHttpServerRunning;
			this.httpServerStart.Enabled = !running;
			this.httpServerStop.Enabled = running;
			this.httpServerRestart.Enabled = running;
		}

		private void setActiveItem(ToolStripMenuItem item)
		{
			if (this.activeVersion != null) this.activeVersion.Checked = false;
			this.activeVersion = item;
			this.activeVersion.Checked = true;
		}

		private async void attempt(string description, Func<Task> action)
		{
			this.notifyIconMenu.Enabled = false;
			this.waitingForm.description.Text = "Waiting for " + description + "...";
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
				if (button == System.Windows.Forms.DialogResult.Retry) attempt(description, action);
			}
			catch (HttpServerStopFailedException)
			{
				this.waitingForm.Hide();
				var serviceName = Properties.Settings.Default.HttpServerServiceName;
				var button = MessageBox.Show("Unable to stop " + serviceName + " service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
				if (button == System.Windows.Forms.DialogResult.Retry) attempt(description, action);
			}
			finally
			{
				this.updateHttpServerMenuState();
				this.notifyIconMenu.Enabled = true;
			}
		}

		private void version_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			var version = (Version)menuItem.Tag;

			attempt("PHP version to change", async () =>
			{
				await this.model.SwitchTo(version);
				this.setActiveItem(menuItem);
			});
		}

		private void httpServerStart_Clicked(object sender, EventArgs e)
		{
			attempt("HTTP server to start", async () =>
			{
				await this.model.StartHttpServer();
			});
		}

		private void httpServerStop_Clicked(object sender, EventArgs e)
		{
			attempt("HTTP server to stop", async () =>
			{
				await this.model.StopHttpServer();
			});
		}

		private void httpServerRestart_Clicked(object sender, EventArgs e)
		{
			attempt("HTTP server to restart", async () =>
			{
				await this.model.StartHttpServer();
				await this.model.StopHttpServer();
			});
		}

		private void refresh_Clicked(object sender, EventArgs e)
		{
			this.initMainMenu();
		}

		private void close_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}
