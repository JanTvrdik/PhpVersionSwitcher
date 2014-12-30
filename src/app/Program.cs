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
					processManagers.Add(new ProcessManager(settings.PhpDir + "\\active\\php-cgi.exe", "-b " + settings.FastCgiAddress.Trim(), "PHP FastCGI"));
				}
				else if (settings.FastCgiAddresses.Count > 0)
				{
					foreach (var FastCgiAddress in settings.FastCgiAddresses)
					{
						processManagers.Add(new ProcessManager(settings.PhpDir + "\\active\\php-cgi.exe", "-b " + FastCgiAddress.Trim(), "PHP FastCGI (" + FastCgiAddress.Substring(FastCgiAddress.IndexOf(':') + 1) + ")"));
					}
				}

				if (settings.PhpServerDocumentRoot.Length + settings.PhpServerAddress.Length > 0)
				{
					processManagers.Add(new ProcessManager(settings.PhpDir + "\\active\\php.exe", "-S " + settings.PhpServerAddress + " -t " + settings.PhpServerDocumentRoot, "PHP built-in server"));
				}

				var phpVersions = new VersionsManager(settings.PhpDir, processManagers);
				var waitingForm = new WaitingForm();
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
