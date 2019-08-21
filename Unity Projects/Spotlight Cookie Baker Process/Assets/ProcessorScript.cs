
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;

using UnityEngine;


namespace FCT.CookieBakerRT.SpotlightProcessing
{
	public class ProcessorScript 
		: MonoBehaviour
	{

		#region Attributes

		public	ComputeShader		ComputeShader;

		private int					_incommingMessagePortNumber;
		private int					_outgoingMessagePortNumber;
		private BackgroundWorker	_udpBackgoundMessenger;
		private bool				_runUDPLoop					= false;

		#endregion


		private void Awake()
		{
			var commandLineArgs = System.Environment.GetCommandLineArgs();

			var portInFound		= false;
			var portOutFound	= false;
			var argumentCount	= commandLineArgs.Length;


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

			if (!portInFound && !portOutFound)
				Application.Quit(-1);
			if (!portInFound)
				Application.Quit(-2);
			if (!portOutFound)
				Application.Quit(-3);

			_runUDPLoop = true;
		}

		private void Start()
		{
			_udpBackgoundMessenger = new BackgroundWorker();
			_udpBackgoundMessenger.DoWork += UDP_BackgroundThread;

			_udpBackgoundMessenger.RunWorkerAsync();
		}

		private void UDP_BackgroundThread(object sender, DoWorkEventArgs e)
		{
			var endpointToListenTo	= new IPEndPoint(IPAddress.Any, _incommingMessagePortNumber);
			var endpointOutgoing	= new IPEndPoint(IPAddress.Loopback, _outgoingMessagePortNumber);

			// I know they have to call it something, and I'll admit that the options are limited, but did they 
			// have to call it UdpClient when it can run as both a udp-client and a udp-server?
			// -FCT
			using (var udp = new UdpClient(endpointToListenTo))
			{

				var byteArray = CreateUpAndRunningMessage();

				udp.Close();
			}
		}

		private byte[] CreateUpAndRunningMessage()
		{
			throw new System.NotImplementedException();
		}
	}
}
