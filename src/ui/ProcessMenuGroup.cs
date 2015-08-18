using System.Windows.Forms;
using System.Collections.Generic;
using PhpVersionSwitcher.Properties;

namespace PhpVersionSwitcher
{
	class ProcessMenuGroup : ToolStripMenuItem
	{
		public ToolStripItem StartItem { get; private set; }
		public ToolStripItem StopItem { get; private set; }
		public ToolStripItem RestartItem { get; private set; }
		public List<ProcessMenu> ProcessMenus { get; private set; }

		public ProcessMenuGroup(string name, List<ProcessMenu> processMenus) : base(name)
		{
			this.ProcessMenus = processMenus;

			foreach (var processMenu in processMenus)
			{
				this.DropDownItems.Add(processMenu);
			}

			this.DropDownItems.Add(new ToolStripSeparator());
			this.StartItem = this.DropDownItems.Add("Start", Resources.Start);
			this.StopItem = this.DropDownItems.Add("Stop", Resources.Stop);
			this.RestartItem = this.DropDownItems.Add("Restart", Resources.Restart);
			this.Refresh();
		}

		public void Refresh()
		{
			bool running = false;
			foreach (var processMenu in this.ProcessMenus)
			{
				if (processMenu.ProcessManager.IsRunning())
				{
					running = true;
					break;
				}
			}

			this.Image = running ? Resources.Start : Resources.Stop;
			this.StartItem.Enabled = !running;
			this.StopItem.Enabled = running;
			this.RestartItem.Enabled = running;
		}

	}
}
