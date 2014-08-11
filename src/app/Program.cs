using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PhpVersionSwitcher.Properties;

namespace PhpVersionSwitcher
{
	internal static class Program
	{
		/// <summary>
		///     The main entry point for the application.
		/// </summary>
		[STAThread]
		private static void Main()
		{
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				var settings = Settings.Default;
				var processManagers = new List<IProcessManager>();
				var phpVersions = new VersionsManager(settings.PhpDir, processManagers);
				var waitingForm = new WaitingForm();

				if (settings.HttpServerServiceName.Trim().Length > 0)
				{
					processManagers.Add(new ServiceManager(settings.HttpServerServiceName));
				}

				if (settings.HttpServerProcessPath.Trim().Length > 0)
				{
					processManagers.Add(new ProcessManager(settings.HttpServerProcessPath));
				}

				if (settings.FastCgiAddress.Trim().Length > 0)
				{
					processManagers.Add(new ProcessManager(settings.PhpDir + "\\active\\php-cgi.exe", "-b " + settings.FastCgiAddress));
				}

				new MainForm(processManagers, phpVersions, waitingForm);
				Application.Run();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Something went wrong!\n" + ex.Message, "Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
