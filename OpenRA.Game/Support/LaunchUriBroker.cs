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
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRA
{
	/// <summary>
	/// Forwards protocol launch URIs to the first running YMCA process.
	/// Regular launches are not forwarded, so advanced users can still run
	/// multiple clients intentionally.
	/// </summary>
	public sealed class LaunchUriBroker : IDisposable
	{
		const string MutexName = "OpenRA-YMCA-LaunchUri-v1";
		const string PipeName = "openra-ymca-launch-uri-v1";

		readonly Mutex mutex;
		readonly CancellationTokenSource cancellation = new();
		readonly Action<string> onUriReceived;
		readonly bool ownsMutex;

		public LaunchUriBroker(Action<string> onUriReceived)
		{
			this.onUriReceived = onUriReceived;
			mutex = new Mutex(false, MutexName);
			try
			{
				ownsMutex = mutex.WaitOne(0);
			}
			catch (AbandonedMutexException)
			{
				ownsMutex = true;
			}

			if (ownsMutex)
				_ = ListenAsync();
		}

		public bool ForwardToRunningInstance(string uri)
		{
			if (ownsMutex || string.IsNullOrEmpty(uri))
				return false;

			try
			{
				using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
				client.Connect(2000);
				using var writer = new StreamWriter(client) { AutoFlush = true };
				writer.WriteLine(uri);
				return true;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to forward launch URI to the running YMCA instance: {ex.Message}");
				return false;
			}
		}

		async Task ListenAsync()
		{
			while (!cancellation.IsCancellationRequested)
			{
				try
				{
					using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
						PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
					await server.WaitForConnectionAsync(cancellation.Token);
					using var reader = new StreamReader(server);
					var uri = await reader.ReadLineAsync();
					if (!string.IsNullOrEmpty(uri))
						onUriReceived(uri);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (ObjectDisposedException)
				{
					break;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"Failed to receive a forwarded launch URI: {ex.Message}");
					await Task.Delay(250);
				}
			}
		}

		public void Dispose()
		{
			cancellation.Cancel();
			cancellation.Dispose();
			if (ownsMutex)
				mutex.ReleaseMutex();
			mutex.Dispose();
		}
	}
}
