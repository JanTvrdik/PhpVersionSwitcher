using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using PhpVersionSwitcher.Properties;

namespace PhpVersionSwitcher
{
	public partial class MainForm : Form
	{
		private List<ProcessMenu> submenus;
		private WaitingForm waitingForm;
		private ToolStripMenuItem activeVersionItem;
		private VersionsManager phpVersions;

		public MainForm()
		{
			this.submenus = new List<ProcessMenu>();
			this.waitingForm = new WaitingForm();

			IProcessManager server = null;

			try
			{
				if (Settings.Default.HttpServerServiceName.Trim().Length > 0)
				{
					server = new ServiceManager(Settings.Default.HttpServerServiceName);
					this.submenus.Add(new ProcessMenu(this, server));
				}

				if (Settings.Default.HttpServerProcessPath.Trim().Length > 0)
				{
					server = new ProcessManager(Settings.Default.HttpServerProcessPath);
					this.submenus.Add(new ProcessMenu(this, server));
				}
			
				if (Settings.Default.FastCgiAddress.Trim().Length > 0)
				{
					server = new ProcessManager(Settings.Default.PhpDir + "\\active\\php-cgi.exe", "-b " + Settings.Default.FastCgiAddress);
					this.submenus.Add(new ProcessMenu(this, server));
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

			foreach (var menu in this.submenus)
			{
				this.notifyIconMenu.Items.Add(menu);
			}

			this.notifyIconMenu.Items.Add(new ToolStripSeparator());
			this.notifyIconMenu.Items.Add("Refresh", null, this.refresh_Clicked);
			this.notifyIconMenu.Items.Add("Close", null, this.close_Click);
		}

		private void SetActiveItem(ToolStripMenuItem item)
		{
			if (this.activeVersionItem != null) this.activeVersionItem.Checked = false;
			this.activeVersionItem = item;
			this.activeVersionItem.Checked = true;
		}

		public async void Attempt(string description, Func<Task> action)
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
					var dialogResult = MessageBox.Show("Unable to " + ex.Operation + " " + ex.Name + ".", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					if (dialogResult != DialogResult.Retry) break;
				}
			}
			
			this.RefreshSubMenus();
			this.waitingForm.Hide();
			this.notifyIconMenu.Enabled = true;
		}

		private void RefreshSubMenus()
		{
			foreach (var menu in this.submenus)
			{
				menu.Refresh();
			}
		}

		private void ShowFatalError(string message)
		{
			MessageBox.Show(message, "Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			Application.Exit();
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

		private void refresh_Clicked(object sender, EventArgs e)
		{
			this.InitializeMainMenu();
		}

		private void close_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}
