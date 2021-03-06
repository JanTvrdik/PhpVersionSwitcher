﻿using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PhpVersionSwitcher
{
	internal class Version : IComparable<Version>, IEquatable<Version>
	{
		public enum VersionStability
		{
			Dev = 0,
			Alpha = 1,
			Beta = 2,
			RC = 3,
			Stable = 4
		};

		public Version(int major, int minor, int patch, VersionStability stability, int stabilityVersion, string label)
		{
			this.Major = major;
			this.Minor = minor;
			this.Patch = patch;
			this.Stability = stability;
			this.StabilityVersion = stabilityVersion;
			this.Label = label;
		}

		public int Major { get; private set; }

		public int Minor { get; private set; }

		public int Patch { get; private set; }

		public string Full { get { return Major + "." + Minor + "." + Patch; } }

		public VersionStability Stability { get; private set; }

		public int StabilityVersion { get; private set; }

		public string Label { get; private set; }

		public int CompareTo(Version other)
		{
			if (this.Major != other.Major) return this.Major - other.Major;
			if (this.Minor != other.Minor) return this.Minor - other.Minor;
			if (this.Patch != other.Patch) return this.Patch - other.Patch;
			if (this.Stability != other.Stability) return this.Stability - other.Stability;
			if (this.StabilityVersion != other.StabilityVersion) return this.StabilityVersion - other.StabilityVersion;
			return this.Label.CompareTo(other.Label);
		}

		public bool Equals(Version other)
		{
			return other != null && this.CompareTo(other) == 0;
		}

		public static bool TryParse(string label, out Version version)
		{
			Match match = Regex.Match(label, @"^(\d+)\.(\d+)\.(\d+)(?:-(dev|(alpha|beta|rc)(\d+)))?", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				version = null;
				return false;
			}

			var major = Int32.Parse(match.Groups[1].Value);
			var minor = Int32.Parse(match.Groups[2].Value);
			var patch = Int32.Parse(match.Groups[3].Value);
			var stability = VersionStability.Stable;
			var stabilityVersion = 0;

			if (match.Groups[5].Success)
			{
				stability = (VersionStability) Enum.Parse(typeof(VersionStability), match.Groups[5].Value, true);
				stabilityVersion = Int32.Parse(match.Groups[6].Value);
			}
			else if (match.Groups[4].Success)
			{
				stability = VersionStability.Dev;
			}

			version = new Version(major, minor, patch, stability, stabilityVersion, label);
			return true;
		}
	}
}
