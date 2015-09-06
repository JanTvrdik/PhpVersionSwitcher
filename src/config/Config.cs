using System.Collections.Generic;

namespace PhpVersionSwitcher
{
	internal struct Config
	{
		public string PhpDir;
		public List<Service> Services;
		public List<Executable> Executables;


		internal struct Service
		{
			public string Label;
			public string Name;
		}


		internal struct Executable
		{
			public string Label;
			public string Path;
			public string Args;
			public List<Executable> Multiple;
		}
	}
}
