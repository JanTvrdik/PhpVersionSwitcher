using System;
using System.Windows.Forms;
using PhpVersionSwitcher.Properties;

namespace PhpVersionSwitcher
{
	class ProcessMenu : ToolStripMenuItem
	{
		private readonly MainForm form;
		private readonly IProcessManager processManager;

		private ToolStripItem startItem;
		private ToolStripItem stopItem;
		private ToolStripItem restartItem;

		public ProcessMenu(MainForm form, IProcessManager processManager)
			: base(processManager.Name)
		{
			this.form = form;
			this.processManager = processManager;

			this.startItem = this.DropDownItems.Add("Start", Resources.Start, this.startItem_Clicked);
			this.stopItem = this.DropDownItems.Add("Stop", Resources.Stop, this.stopItem_Clicked);
			this.restartItem = this.DropDownItems.Add("Restart", Resources.Restart, this.restartItem_Clicked);
			this.Refresh();
		}

		public void Refresh()
		{
			bool running = this.processManager.IsRunning();
			this.Image = running ? Resources.Start : Resources.Stop;
			this.startItem.Enabled = !running;
			this.stopItem.Enabled = running;
			this.restartItem.Enabled = running;
		}

		private void startItem_Clicked(object sender, EventArgs e)
		{
			this.form.Attempt("HTTP server to start", this.processManager.Start);
		}

		private void stopItem_Clicked(object sender, EventArgs e)
		{
			this.form.Attempt("HTTP server to stop", this.processManager.Stop);
		}

		private void restartItem_Clicked(object sender, EventArgs e)
		{
			this.form.Attempt("HTTP server to restart", this.processManager.Restart);
		}
	}
}
