using System.Windows.Forms;
using PhpVersionSwitcher.Properties;

namespace PhpVersionSwitcher
{
	class ProcessMenu : ToolStripMenuItem
	{
		public IProcessManager ProcessManager { get; private set; }
		public ToolStripItem StartItem { get; private set; }
		public ToolStripItem StopItem { get; private set; }
		public ToolStripItem RestartItem { get; private set; }

		public ProcessMenu(IProcessManager processManager) : base(processManager.Name)
		{
			this.ProcessManager = processManager;
			this.StartItem = this.DropDownItems.Add("Start", Resources.Start);
			this.StopItem = this.DropDownItems.Add("Stop", Resources.Stop);
			this.RestartItem = this.DropDownItems.Add("Restart", Resources.Restart);
			this.Refresh();
		}

		public void Refresh()
		{
			bool running = this.ProcessManager.IsRunning();
			this.Image = running ? Resources.Start : Resources.Stop;
			this.StartItem.Enabled = !running;
			this.StopItem.Enabled = running;
			this.RestartItem.Enabled = running;
		}
	}
}
