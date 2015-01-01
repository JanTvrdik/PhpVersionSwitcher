using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
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
					var processes = new List<ProcessManager>()
					{
						new ProcessManager(settings.HttpServerProcessPath)
					};
					injectRunningProcesses(processes, processes[0].FileName);
					processManagers.AddRange(processes);
				}

				if (settings.FastCgiAddress.Trim().Length > 0)
				{
					var processes = new List<ProcessManager>()
					{
						new ProcessManager(settings.PhpDir + "\\active\\php-cgi.exe", "-b " + settings.FastCgiAddress.Trim(), "PHP FastCGI")
					};
					injectRunningProcesses(processes, processes[0].FileName);
					processManagers.AddRange(processes);
				}
				else if (settings.FastCgiAddresses.Count > 0)
				{
					var processes = new List<ProcessManager>();
					foreach (var FastCgiAddress in settings.FastCgiAddresses)
					{
						processes.Add(new ProcessManager(settings.PhpDir + "\\active\\php-cgi.exe", "-b " + FastCgiAddress.Trim(), "PHP FastCGI (" + FastCgiAddress.Substring(FastCgiAddress.IndexOf(':') + 1) + ")", "PHP FastCGI"));
					}
					injectRunningProcesses(processes, processes[0].FileName);
					processManagers.AddRange(processes);
				}

				if (settings.PhpServerDocumentRoot.Length + settings.PhpServerAddress.Length > 0)
				{
					var processes = new List<ProcessManager>()
					{
						new ProcessManager(settings.PhpDir + "\\active\\php.exe", "-S " + settings.PhpServerAddress + " -t " + settings.PhpServerDocumentRoot, "PHP built-in server")
					};
					injectRunningProcesses(processes, processes[0].FileName);
					processManagers.AddRange(processes);
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

		private static void injectRunningProcesses(List<ProcessManager> processManagers, string fileName)
		{
			string wmiQuery = string.Format("select CommandLine, ProcessId from Win32_Process where Name='{0}'", fileName);
			ManagementObjectCollection managementObjects = (new ManagementObjectSearcher(wmiQuery)).Get();
			foreach (ManagementObject managementObject in managementObjects)
			{
				string line = (string)(managementObject["CommandLine"]);
				foreach (var processManager in processManagers)
				{
					if (line != null && line.Contains(processManager.Arguments))
					{
						var pId = Convert.ToInt32(managementObject["ProcessId"]);
						processManager.Process = Process.GetProcessById(pId);
					}
				}
			}
		}

	}
}
