
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using FCT.CookieBakerRT.IPC_DataFormat;
using FlatBuffers;

using UnityEngine;


namespace FCT.CookieBakerRT.SpotlightProcessing
{
	public class ProcessorScript 
		: MonoBehaviour
	{

		#region Attributes

		public	ComputeShader				ComputeShader;

		private int							_incommingMessagePortNumber;
		private int							_outgoingMessagePortNumber;
		private BackgroundWorker			_udpBackgoundMessenger;
		private bool						_runUDPLoop						= false;

		private ConcurrentQueue<Message>	_incommingMessages;
		private ConcurrentQueue<byte[]>		_outgoingMessages;

		#endregion


		private void Awake()
		{
			var commandLineArgs = System.Environment.GetCommandLineArgs();

			var portInFound		= false;
			var portOutFound	= false;
			var argumentCount	= commandLineArgs.Length;

			// Check to see if the parent process sent us the port numbers needed for IPC.
			for (int i = 0; i < argumentCount - 1; i++)
			{
				if (commandLineArgs[i] == "-inputPort")
				{
					bool worked = int.TryParse(commandLineArgs[i + 1], out int portNum);
					if (worked)
					{
						_incommingMessagePortNumber = portNum;
						portInFound = true;
					}
				}
				else if (commandLineArgs[i] == "-outputPort")
				{
					bool worked = int.TryParse(commandLineArgs[i + 1], out int portNum);
					if (worked)
					{
						_outgoingMessagePortNumber = portNum;
						portOutFound = true;
					}
				}
			}

			// We're missing at least one port number
			if (!portInFound && !portOutFound)
				Application.Quit(-1);
			if (!portInFound)
				Application.Quit(-2);
			if (!portOutFound)
				Application.Quit(-3);

			// Port numbers found, better get things up and running.
			_runUDPLoop			= true;
			_incommingMessages	= new ConcurrentQueue<Message>();
			_outgoingMessages	= new ConcurrentQueue<byte[]>();
		}

		private void Start()
		{
			_udpBackgoundMessenger = new BackgroundWorker();
			_udpBackgoundMessenger.DoWork += UDP_BackgroundThread;

			_udpBackgoundMessenger.RunWorkerAsync();
		}

		private void Update()
		{
		}

		private void UDP_BackgroundThread(object sender, DoWorkEventArgs e)
		{
			var endpointToListenTo	= new IPEndPoint(IPAddress.Any, _incommingMessagePortNumber);
			var endpointOutgoing	= new IPEndPoint(IPAddress.Loopback, _outgoingMessagePortNumber);

			// I know they have to call it something, and I'll admit that the options are limited, but did they 
			// have to call it UdpClient when it can run as both a udp-client and a udp-server?
			// -FCT
			using (var udpListen	= new UdpClient(endpointToListenTo) { DontFragment = true, ExclusiveAddressUse = false })
			using (var udpSend		= new UdpClient() { DontFragment = true, ExclusiveAddressUse = false })
			{
				// Let the parent process know that we're up and running.
				var byteArray = CreateUpAndRunningMessage();
				udpSend.Send(byteArray, byteArray.Length, endpointOutgoing);

				// Enter our normal comunications loop
				while (_runUDPLoop)
				{
					// Grab valid incoming messages and send them to the main thread for processing.
					while (udpListen.Available > 0)
					{
						byteArray	= udpListen.Receive(ref endpointToListenTo);
						var buffer	= new ByteBuffer(byteArray);
						var message = Message.GetRootAsMessage(buffer);

						switch (message.DataType)
						{
							case MessageDatum.CancelWorkload:
							case MessageDatum.WorkloadRequest:
								_incommingMessages.Enqueue(message);
								break;

							// If you are getting any of these, then something messed up is going on with the parent process.
							case MessageDatum.NONE:
							case MessageDatum.ProgressUpdate:
							case MessageDatum.WorkloadComplete:
							case MessageDatum.WorkloadReceived:
								break;
						}
					}

					// Grab outgoing messages from the main thread and send them to the parent processes.
					while (_outgoingMessages.TryDequeue(out byte[] messageOut))
					{
						udpSend.Send(messageOut, messageOut.Length, endpointOutgoing);
					}

					// Sleep for a bit to stop this thread from taking up too much resources.
					Thread.Sleep(3);
				}	// Exit udp loop

				udpListen.Close();
				udpSend.Close();
			}	// Dispose of UdpClients
		}

		private byte[] CreateUpAndRunningMessage()
		{
			throw new System.NotImplementedException();
		}
	}
}
