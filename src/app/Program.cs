using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Windows.Forms;
using PhpVersionSwitcher.Properties;
using System.IO;
using Newtonsoft.Json.Linq;
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

				string settingsFile = System.Reflection.Assembly.GetEntryAssembly().Location;
				settingsFile = settingsFile.Substring(0, settingsFile.Length - 3) + "json";
				JObject settings = JObject.Parse(File.ReadAllText(settingsFile));

				settings["managers"].ToList().ForEach(manager =>
				{
					IDictionary<string, JToken> managerSetting = (IDictionary<string, JToken>)manager;
					var type = (string)managerSetting["type"];

					if (type == "service")
					{
						processManagers.Add(new ServiceManager(
							(string)managerSetting["name"],
							((string)managerSetting["label"]) ?? null
						));
					}
					else if (type == "executable")
					{
						List<ProcessManager> processes;
						if (!managerSetting.ContainsKey("multiple"))
						{
							processes = new List<ProcessManager>() { new ProcessManager(
								(string)managerSetting["path"],
								((string)managerSetting["args"]) ?? "",
								((string)managerSetting["label"]) ?? null
							) };
						}
						else
						{
							processes = new List<ProcessManager>();
							managerSetting["multiple"].ToList().ForEach(managerInstanceSettings =>
							{
								processes.Add(new ProcessManager(
									(string)managerSetting["path"],
									(string)managerInstanceSettings["args"],
									((string)managerInstanceSettings["label"]) ?? null,
									(string)managerSetting["label"])
								);
							});
						}

						processes.ForEach(process => processManagers.Add(process));
						injectRunningProcesses(processes, processes[0].FileName);
					}
					else
					{
						throw new InvalidDataException();
					}
				});

				var phpVersions = new VersionsManager((string)settings["phpDir"], processManagers);
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
