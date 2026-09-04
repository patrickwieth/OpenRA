#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or (at your
 * option) any later version. For more information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Network;

namespace OpenRA.Test
{
	[TestFixture]
	public sealed class SpectatorHandshakeTest
	{
		[Test]
		public void SerializesExplicitSpectatorRequest()
		{
			var response = new HandshakeResponse
			{
				Mod = "ca",
				Version = "test",
				Password = "spectator-password",
				Spectator = true,
				Client = new Session.Client { Name = "Viewer" }
			};

			var serialized = response.Serialize();
			var parsed = HandshakeResponse.Deserialize(serialized, "handshake");

			Assert.That(parsed.Spectator, Is.True);
			Assert.That(parsed.Password, Is.EqualTo("spectator-password"));
		}

		[Test]
		public void OmitsSpectatorFieldForNormalConnections()
		{
			var response = new HandshakeResponse
			{
				Mod = "ca",
				Version = "test",
				Client = new Session.Client { Name = "Player" }
			};

			Assert.That(response.Serialize(), Does.Not.Contain("Spectator:"));
		}
	}
}
