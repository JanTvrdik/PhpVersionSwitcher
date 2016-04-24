using System;

namespace PhpVersionSwitcher
{
	[Serializable]
	class ProcessException : Exception
	{
		public string Name { get; private set; }
		public string Operation { get; private set; }

		public ProcessException(string name, string operation)
		{
			this.Name = name;
			this.Operation = operation;
		}
	}
}
