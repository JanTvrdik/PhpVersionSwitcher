using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Linq;

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

				var processManagers = new List<IProcessManager>();

				var exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
				var configPath = exePath.Substring(0, exePath.Length - 3) + "json";
				var config = new ConfigLoader().Load(configPath);

				config.Services?.ForEach(service =>
				{
					processManagers.Add(new ServiceManager(service.Name, service.Label));
				});

				config.Executables?.ForEach(exe =>
				{
					List<ProcessManager> processes;

					if (exe.Multiple == null)
					{
						processes = new List<ProcessManager>() {
							new ProcessManager(exe.Path, exe.Args, exe.Label)
						};
					}
					else
					{
						processes = exe.Multiple
							.Select(child => new ProcessManager(child.Path, child.Args, child.Label, exe.Label))
							.ToList();
					}

					processes.ForEach(process => processManagers.Add(process));
					injectRunningProcesses(processes, processes[0].FileName);
				});

				var phpVersions = new VersionsManager(config.PhpDir, processManagers);
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
			var list = new Dictionary<ProcessManager,List<Tuple<int,int>>>();
			foreach (var processManager in processManagers)
			{
				list.Add(processManager, new List<Tuple<int,int>>());
			}

			string wmiQuery = string.Format("select CommandLine, ProcessId, ParentProcessID from Win32_Process where Name='{0}'", fileName);
			ManagementObjectCollection managementObjects = (new ManagementObjectSearcher(wmiQuery)).Get();
			foreach (ManagementObject managementObject in managementObjects)
			{
				string line = (string)(managementObject["CommandLine"]);
				foreach (var processManager in processManagers)
				{
					if (line != null && line.Contains(processManager.Arguments))
					{
						var pId = Convert.ToInt32(managementObject["ProcessId"]);
						var parentPId = Convert.ToInt32(managementObject["ParentProcessId"]);
						list[processManager].Add(new Tuple<int, int>(pId, parentPId));
					}
				}
			}

			foreach (var processManager in processManagers)
			{
				int pId = -1;
				var pairs = list[processManager];

				if (pairs.Count == 1)
				{
					pId = pairs.ToArray()[0].Item1;
				}
				else if (pairs.Count > 1)
				{
					var parentIds = new List<int>();
					foreach (var pair in pairs)
					{
						parentIds.Add(pair.Item1);
					}
					foreach (var pair in pairs)
					{
						if (!parentIds.Contains(pair.Item2))
						{
							pId = pair.Item1;
						}
					}
				}

				if (pId == -1)
				{
					continue;
				}

				processManager.Process = Process.GetProcessById(pId);
			}
		}

	}
}
