#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or (at your
 * option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	[NonParallelizable]
	public sealed class LaunchUriBrokerTest
	{
		[Test]
		public void ForwardsUriToPrimaryInstance()
		{
			var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
			using var primary = new LaunchUriBroker(uri => received.TrySetResult(uri));
			const string Uri = "ymca://example.test:1234?password=secret&name=Player";

			var forwarded = Task.Run(() =>
			{
				using var secondary = new LaunchUriBroker(_ => { });
				return secondary.ForwardToRunningInstance(Uri);
			}).GetAwaiter().GetResult();

			Assert.That(forwarded, Is.True);
			Assert.That(received.Task.WaitAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult(), Is.EqualTo(Uri));
		}
	}
}
