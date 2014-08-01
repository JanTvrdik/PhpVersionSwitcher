using System;
using System.Windows.Forms;

namespace PhpVersionSwitcher
{
    public partial class MainForm : Form
    {
        private Model model;
        private ToolStripMenuItem activeVersion;
        private ToolStripItem apacheStart;
        private ToolStripItem apacheStop;
        private ToolStripItem apacheRestart;

        public MainForm()
        {
            InitializeComponent();
            this.notifyIcon.Icon = this.Icon;
            this.model = new Model(Properties.Settings.Default.PhpDir, Properties.Settings.Default.HttpServerServiceName);
            this.init();
        }

        private void init()
        {
            var activeVersion = this.model.ActiveVersion;
            var versions = this.model.AvailableVersions;

            this.notifyIconMenu.Items.Clear();
            foreach (var version in versions)
            {
                var item = new ToolStripMenuItem(version.ToString(), null, new EventHandler(version_Clicked));
                item.Tag = version;

                if (version.Equals(activeVersion))
                {
                    this.setActiveItem(item);
                }

                this.notifyIconMenu.Items.Add(item);
            }
            this.notifyIconMenu.Items.Add(new ToolStripSeparator());
            this.notifyIconMenu.Items.Add(this.getApacheMenu());
            this.notifyIconMenu.Items.Add("Refresh", null, new EventHandler(refresh_Clicked));
            this.notifyIconMenu.Items.Add("Close", null, new EventHandler(close_Click));
        } 

        private ToolStripMenuItem getApacheMenu()
        {
            var menu = new ToolStripMenuItem("Apache");
            this.apacheStart = menu.DropDownItems.Add("Start", null, new EventHandler(apacheStart_Clicked));
            this.apacheStop = menu.DropDownItems.Add("Stop", null, new EventHandler(apacheStop_Clicked));
            this.apacheRestart = menu.DropDownItems.Add("Restart", null, new EventHandler(apacheRestart_Clicked));

            var apacheStatus = this.model.ApacheStatus;
            if (apacheStatus == System.ServiceProcess.ServiceControllerStatus.Running)
            {
                this.apacheStart.Enabled = false;
            }
            else if (apacheStatus == System.ServiceProcess.ServiceControllerStatus.Stopped)
            {
                this.apacheStop.Enabled = false;
                this.apacheRestart.Enabled = false;
            }            

            return menu;
        }

        private void setActiveItem(ToolStripMenuItem item)
        {
            if (this.activeVersion != null) this.activeVersion.Checked = false;
            this.activeVersion = item;
            this.activeVersion.Checked = true;
        }

        private void version_Clicked(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var version = (Version)item.Tag;

			try
			{
				this.model.SwitchTo(version);
                this.setActiveItem(item);
			}
			catch (ApacheStartFailedException)
			{
				var button = MessageBox.Show("Unable to start Apache service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
				if (button == System.Windows.Forms.DialogResult.Retry) version_Clicked(sender, e);
			}
			catch (ApacheStopFailedException)
			{
				var button = MessageBox.Show("Unable to stop Apache service.", "Operation failed", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
				if (button == System.Windows.Forms.DialogResult.Retry) version_Clicked(sender, e);
			}
        }

        private void apacheStart_Clicked(object sender, EventArgs e)
        {
            if (this.model.StartApache())
            {
                this.apacheStart.Enabled = false;
                this.apacheStop.Enabled = true;
                this.apacheRestart.Enabled = true;
            }
        }

        private void apacheStop_Clicked(object sender, EventArgs e)
        {
            if (this.model.StopApache())
            {
                this.apacheStart.Enabled = true;
                this.apacheStop.Enabled = false;
                this.apacheRestart.Enabled = false;
            }
        }

        private void apacheRestart_Clicked(object sender, EventArgs e)
        {
            var success = this.model.StopApache() && this.model.StartApache();
        }

        private void refresh_Clicked(object sender, EventArgs e)
        {
            this.init();
        }

        private void close_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
