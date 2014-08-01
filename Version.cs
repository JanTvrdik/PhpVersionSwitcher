using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	public class Version : IComparable<Version>
	{
		public enum VersionStability { Alpha = 0, Beta = 1, RC = 2, Stable = 3 };

		public int Major
		{
			get;
			private set;
		}

		public int Minor
		{
			get;
			private set;
		}

		public int Patch
		{
			get;
			private set;
		}

		public VersionStability Stability
		{
			get;
			private set;
		}

		public int StabilityVersion
		{
			get;
			private set;
		}

		public Version(int major, int minor, int patch, VersionStability stability, int stabilityVersion)
		{
			Major = major;
			Minor = minor;
			Patch = patch;
			Stability = stability;
			StabilityVersion = stabilityVersion;
		}

		public static bool TryParse(string s, out Version version)
		{
			Match match = Regex.Match(s, @"^(\d+)\.(\d+)\.(\d+)(?:-(alpha|beta|rc)(\d+))?$", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				version = null;
				return false;
			}

			int major = Int32.Parse(match.Groups[1].Value);
			int minor = Int32.Parse(match.Groups[2].Value);
			int patch = Int32.Parse(match.Groups[3].Value);
			var stability = VersionStability.Stable;
			int stabilityVersion = 0;

			if (match.Groups[4].Success)
			{
				stability = (VersionStability)Enum.Parse(typeof(VersionStability), match.Groups[4].Value, true);
				stabilityVersion = Int32.Parse(match.Groups[5].Value);
			}

			version = new Version(major, minor, patch, stability, stabilityVersion);
			return true;
		}

		public bool Equals(Version other)
		{
			return this.CompareTo(other) == 0;
		}

		public int CompareTo(Version other)
		{
			if (this.Major != other.Major) return this.Major - other.Major;
			else if (this.Minor != other.Minor) return this.Minor - other.Minor;
			else if (this.Patch != other.Patch) return this.Patch - other.Patch;
			else if (this.Stability != other.Stability) return this.Stability - other.Stability;
			else return this.StabilityVersion - other.StabilityVersion;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(this.Major);
			sb.Append('.');
			sb.Append(this.Minor);
			sb.Append('.');
			sb.Append(this.Patch);

			if (this.Stability != VersionStability.Stable)
			{
				sb.Append('-');
				sb.Append(this.Stability.ToString().ToLower());
				sb.Append(this.StabilityVersion);
			}

			return sb.ToString();
		}
	}
}
