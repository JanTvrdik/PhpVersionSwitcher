﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	class VersionsManager
	{
		private string phpBaseDir;

		private ServiceManager httpServer;

		public VersionsManager(string phpBaseDir, ServiceManager httpServiceManager)
		{
			this.phpBaseDir = phpBaseDir;
			this.httpServer = httpServiceManager;
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

		public async Task SwitchTo(Version version)
		{
			await this.httpServer.Stop();
			await this.UpdateSymlink(version);
			await this.UpdatePhpIni(version);
			await this.httpServer.Start();
		}

		private string ActivePhpDir
		{
			get { return this.phpBaseDir + "\\active"; }
		}

		private string ConfigurationDir
		{
			get { return this.phpBaseDir + "\\configuration"; }
		}

		private string VersionsDir
		{
			get { return this.phpBaseDir + "\\versions"; }
		}

		private string GetVersionDir(Version version)
		{
			return this.VersionsDir + "\\" + version;
		}

		private Task UpdatePhpIni(Version version)
		{
			return Task.Run(() =>
			{
				var files = new string[]
				{
					version.Major + ".x.x.ini",
					version.Major + "." + version.Minor + ".x.ini",
					version.Major + "." + version.Minor + "." + version.Patch + ".ini"
				};

				var ini = new StringBuilder();
				foreach (var file in files)
				{
					var path = this.ConfigurationDir + "\\" + file;
					if (File.Exists(path))
					{
						var content = File.ReadAllText(path);
						content = content.Replace("%phpDir%", this.GetVersionDir(version));
						ini.AppendLine();
						ini.AppendLine(content);
					}
				}

				File.WriteAllText(this.ActivePhpDir + "\\php.ini", ini.ToString());
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
	}
}