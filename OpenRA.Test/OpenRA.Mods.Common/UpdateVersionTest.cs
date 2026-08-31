#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Common;

namespace OpenRA.Test
{
	[TestFixture]
	public sealed class UpdateVersionTest
	{
		[TestCase("v0.96.18", "v0.96.17", true)]
		[TestCase("0.97.0", "v0.96.17", true)]
		[TestCase("v0.96.17", "v0.96.17", false)]
		[TestCase("v0.96.16", "v0.96.17", false)]
		[TestCase("development-hash", "v0.96.17", false)]
		public void ComparesReleaseVersions(string available, string current, bool expected)
		{
			Assert.That(WebServices.IsNewerVersion(available, current), Is.EqualTo(expected));
		}
	}
}
