using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhpVersionSwitcher
{
	class ConfigLoader
	{
		public Config Load(String path)
		{
			var content = File.ReadAllText(path);
			var contentJson = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline); // remove comments

			var config = new Config();
			var root = (JObject) JToken.Parse(contentJson);

			config.PhpDir = (string) (root["phpDir"] ?? Missing(root, "phpDir"));
			config.Services = root["services"]?.Select(this.toService).ToList();
			config.Executables = root["executables"]?.Select(this.toExecutable).ToList();

			return config;
		}

		private Config.Service toService(JToken token)
		{
			var obj = (JObject) token;
			var service = new Config.Service();

			service.Label = (string) (obj["label"] ?? Missing(obj, "label"));
			service.Name = (string) (obj["name"] ?? Missing(obj, "name"));

			return service;
		}

		private Config.Executable toExecutable(JToken token)
		{
			return this.toExecutable(token, null);
		}

		private Config.Executable toExecutable(JToken token, Config.Executable? parent)
		{
			var obj = (JObject) token;
			var exe = new Config.Executable();

			exe.Label = (string) (obj["label"] ?? Missing(obj, "label"));
			exe.Path = (string) (obj["path"] ?? parent?.Path ?? Missing(obj, "path"));
			exe.Args = (string) (obj["args"] ?? parent?.Args ?? "");

			exe.Env = parent?.Env ?? new Dictionary<string, string>();
			var env = (JObject) obj["env"] ?? new JObject();
			foreach (var pair in env) {
				exe.Env.Add(pair.Key, (string) pair.Value);
			}

			exe.Multiple = obj["multiple"]?.Select(entry => this.toExecutable(entry, exe)).ToList();

			return exe;
		}

		private string Missing(JToken token, string key)
		{
			throw new Exception(string.Format("Config: Missing key '{0}' in {1}.", key, token.Path));
		}
	}
}
