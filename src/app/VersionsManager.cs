using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	internal class VersionsManager
	{
		private string phpBaseDir;

		private IEnumerable<IProcessManager> serverManagers;

		private IEnumerable<IProcessManager> running;

		private bool switchToSuccess;

		public VersionsManager(string phpBaseDir, IEnumerable<IProcessManager> serverManagers)
		{
			this.phpBaseDir = phpBaseDir;
			this.serverManagers = serverManagers;
			this.switchToSuccess = true;
		}

		public SortedSet<Version> GetAvailable()
		{
			var versions = new SortedSet<Version>();

			try
			{
				var dirs = Directory.EnumerateDirectories(this.VersionsDir); // may throw exception
				foreach (var dir in dirs)
				{
					var info = new DirectoryInfo(dir);
					Version version;
					if (File.Exists(dir + "\\php.exe") && Version.TryParse(info.Name, out version))
					{
						versions.Add(version);
					}
				}
			}
			catch (SystemException) { }

			return versions;
		}

		public Version GetActive()
		{
			try
			{
				var target = Symlinks.GetTarget(this.ActivePhpDir); // may throw exception
				var name = new DirectoryInfo(target).Name;
				Version version;
				Version.TryParse(name, out version);
				return version;
			}
			catch (System.ComponentModel.Win32Exception)
			{
				return null;
			}
		}

		public Task SwitchTo(Version version)
		{
			return Task.Run(async () =>
			{
				if (this.switchToSuccess)
				{
					this.running = this.serverManagers.Where(server => server.IsRunning()).ToArray();
				}

				this.switchToSuccess = false;
				await Task.WhenAll(this.running.Select(server => server.Stop()));
				await Task.WhenAll(
					this.UpdateSymlink(version),
					this.UpdatePhpIni(version),
					this.UpdateEnvironmentVariables(version)
				);
				await Task.WhenAll(this.running.Select(server => server.Start()));
				this.switchToSuccess = true;
			});
		}

		public string ActivePhpDir
		{
			get { return this.phpBaseDir + "\\active"; }
		}

		private string ConfigurationDir
		{
			get { return this.phpBaseDir + "\\configurations"; }
		}

		private string VersionsDir
		{
			get { return this.phpBaseDir + "\\versions"; }
		}

		private string GetVersionDir(Version version)
		{
			return this.VersionsDir + "\\" + version.Label;
		}

		private Task UpdatePhpIni(Version version)
		{
			return Task.Run(() =>
			{
				var files = new[]
				{
					version.Major + ".x.x.ini",
					version.Major + "." + version.Minor + ".x.ini",
					version.Major + "." + version.Minor + "." + version.Patch + ".ini",
					version.Label + ".ini"
				};

				var dir = this.GetVersionDir(version);
				var ini = new StringBuilder();
				foreach (var file in files.Distinct())
				{
					var path = this.ConfigurationDir + "\\" + file;
					if (File.Exists(path))
					{
						var content = File.ReadAllText(path);
						content = content.Replace("%phpDir%", dir);
						ini.AppendLine();
						ini.AppendLine(content);
					}
				}

				File.WriteAllText(dir + "\\php.ini", ini.ToString());
			});
		}

		private Task UpdateSymlink(Version version)
		{
			return Task.Run(() =>
			{
				var symlink = this.ActivePhpDir;
				var target = this.GetVersionDir(version);

				try
				{
					Directory.Delete(symlink, true);
				}
				catch (DirectoryNotFoundException) { }

				Symlinks.CreateDir(symlink, target);
			});
		}

		private Task UpdateEnvironmentVariables(Version version)
		{
			return Task.Run(() =>
			{
				var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
				var segments = currentPath.Split(';');
				var inPath = segments.Any((segment) => segment.Equals(this.ActivePhpDir, StringComparison.InvariantCultureIgnoreCase));

				if (!inPath)
				{
					var newSegments = segments.ToList();
					newSegments.Add(this.ActivePhpDir);
					var newPath = String.Join(";", newSegments);

					Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
				}

				var currentMajorVersion = Environment.GetEnvironmentVariable("PHP_VERSION_MAJOR", EnvironmentVariableTarget.Machine);
				var newMajorVersion = version.Major.ToString();

				if (newMajorVersion != currentMajorVersion)
				{
					Environment.SetEnvironmentVariable("PHP_VERSION_MAJOR", newMajorVersion, EnvironmentVariableTarget.Machine);
				}
			});
		}
	}
}
