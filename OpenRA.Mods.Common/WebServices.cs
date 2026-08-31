#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OpenRA.Support;

namespace OpenRA.Mods.Common
{
	public enum ModVersionStatus { NotChecked, Latest, Outdated, Unknown, PlaytestAvailable }

	public class WebServices : IGlobalModData
	{
		public readonly string ServerList = "https://master.openra.net/games";
		public readonly string ServerAdvertise = "https://master.openra.net/ping";
		public readonly string MapRepository = "https://resource.openra.net/map/";
		public readonly string GameNews = "https://master.openra.net/gamenews";
		public readonly string GameNewsFileName = "news.yaml";
		public readonly string VersionCheck = "https://master.openra.net/versioncheck";
		public readonly string UpdateManifest = null;

		public ModVersionStatus ModVersionStatus { get; private set; }
		public string LatestVersion { get; private set; }
		public string UpdateDownloadUrl { get; private set; }
		public string UpdateReleaseUrl { get; private set; }
		public string UpdateSha256 { get; private set; }
		const int VersionCheckProtocol = 1;

		public void CheckModVersion()
		{
			if (!string.IsNullOrEmpty(UpdateManifest))
			{
				CheckUpdateManifest();
				return;
			}

			Task.Run(async () =>
			{
				var queryURL = new HttpQueryBuilder(VersionCheck)
				{
					{ "protocol", VersionCheckProtocol },
					{ "engine", Game.EngineVersion },
					{ "mod", Game.ModData.Manifest.Id },
					{ "version", Game.ModData.Manifest.Metadata.Version }
				}.ToString();

				try
				{
					var client = HttpClientFactory.Create();

					var httpResponseMessage = await client.GetAsync(queryURL);
					var result = await httpResponseMessage.Content.ReadAsStringAsync();

					var status = ModVersionStatus.Latest;
					switch (result)
					{
						case "outdated": status = ModVersionStatus.Outdated; break;
						case "unknown": status = ModVersionStatus.Unknown; break;
						case "playtest": status = ModVersionStatus.PlaytestAvailable; break;
					}

					Game.RunAfterTick(() => ModVersionStatus = status);
				}
				catch { }
			});
		}

		void CheckUpdateManifest()
		{
			Task.Run(async () =>
			{
				try
				{
					var client = HttpClientFactory.Create();
					var data = await client.GetStringAsync(UpdateManifest);
					var values = MiniYaml.FromString(data, UpdateManifest)
						.ToDictionary(node => node.Key, node => node.Value.Value);
					if (!values.TryGetValue("Version", out var version) || string.IsNullOrEmpty(version))
						return;

					var platformKey = Platform.CurrentPlatform switch
					{
						PlatformType.Windows => "WindowsInstaller",
						PlatformType.Linux => "LinuxAppImage",
						PlatformType.OSX => "MacDiskImage",
						_ => null
					};
					values.TryGetValue(platformKey ?? "", out var downloadUrl);
					values.TryGetValue(platformKey == null ? "" : platformKey + "Sha256", out var sha256);
					values.TryGetValue("ReleaseUrl", out var releaseUrl);
					var status = IsNewerVersion(version, Game.ModData.Manifest.Metadata.Version)
						? ModVersionStatus.Outdated : ModVersionStatus.Latest;

					Game.RunAfterTick(() =>
					{
						LatestVersion = version;
						UpdateDownloadUrl = downloadUrl;
						UpdateReleaseUrl = releaseUrl;
						UpdateSha256 = sha256;
						ModVersionStatus = status;
					});
				}
				catch { }
			});
		}

		static bool IsNewerVersion(string available, string current)
		{
			if (!Version.TryParse(available.TrimStart('v', 'V'), out var availableVersion) ||
				!Version.TryParse(current.TrimStart('v', 'V'), out var currentVersion))
				return false;

			return availableVersion > currentVersion;
		}

		public async Task<string> DownloadUpdate()
		{
			if (string.IsNullOrEmpty(UpdateDownloadUrl))
				throw new InvalidOperationException("No update is available for this platform.");

			var extension = Platform.CurrentPlatform switch
			{
				PlatformType.Windows => ".exe",
				PlatformType.Linux => ".AppImage",
				PlatformType.OSX => ".dmg",
				_ => ".download"
			};
			var directory = Path.Combine(Platform.SupportDir, "Updates");
			Directory.CreateDirectory(directory);
			var path = Path.Combine(directory, "YMCA-" + LatestVersion + extension);
			var client = HttpClientFactory.Create();
			using (var source = await client.GetStreamAsync(UpdateDownloadUrl))
			using (var destination = File.Create(path))
				await source.CopyToAsync(destination);

			if (!string.IsNullOrEmpty(UpdateSha256))
			{
				using var sha = SHA256.Create();
				using var stream = File.OpenRead(path);
				var actual = string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2")));
				if (!string.Equals(actual, UpdateSha256, StringComparison.OrdinalIgnoreCase))
				{
					File.Delete(path);
					throw new InvalidDataException("The downloaded update failed verification.");
				}
			}

			return path;
		}
	}
}
