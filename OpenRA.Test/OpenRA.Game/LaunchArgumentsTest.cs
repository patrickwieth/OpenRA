#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class LaunchArgumentsTest
	{
		[Test]
		public void ParsesPasswordFromJoinUri()
		{
			var launch = new LaunchArguments(new Arguments(
				"Launch.URI=ymca://tournament.example.org:1236?password=a%26b+c&name=L%C3%B6rdenMcFairGen%C3%BC"));

			var endpoint = launch.GetConnectEndPoint();
			Assert.That(endpoint.ToString(), Is.EqualTo("tournament.example.org:1236"));
			Assert.That(launch.Password, Is.EqualTo("a&b c"));
			Assert.That(launch.PlayerName, Is.EqualTo("LördenMcFairGenü"));
		}

		[Test]
		public void ExplicitPasswordOverridesJoinUriPassword()
		{
			var launch = new LaunchArguments(new Arguments(
				"Launch.URI=ymca://localhost:1234?password=from-uri&name=from-uri",
				"Launch.Password=from-argument",
				"Launch.PlayerName=from-argument"));

			Assert.That(launch.Password, Is.EqualTo("from-argument"));
			Assert.That(launch.PlayerName, Is.EqualTo("from-argument"));
		}
	}
}
