using System.Windows.Forms;
using System.Collections.Generic;

namespace PhpVersionSwitcher
{
	class ProcessMenuGroup : ToolStripMenuItem
	{
		public ProcessMenuGroup(string name, List<ProcessMenu> processMenus) : base(name)
		{
			foreach (var processMenu in processMenus)
			{
				this.DropDownItems.Add(processMenu);
			}
		}
	}
}
