
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
		}

		private void Start()
		{
			_runUDPLoop				= true;
			_incommingMessages		= new ConcurrentQueue<Message>();
			_outgoingMessages		= new ConcurrentQueue<byte[]>();
			_udpBackgoundMessenger	= new BackgroundWorker();

			_udpBackgoundMessenger.DoWork += UDP_BackgroundThread;
			_udpBackgoundMessenger.RunWorkerAsync();
		}

		private void Update()
		{
			while (_incommingMessages.TryDequeue(out Message message))
			{
				switch (message.DataType)
				{
					case MessageDatum.WorkloadRequest:
						break;

					case MessageDatum.CancelWorkload:
						break;
				}
			}
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
			var builder = new FlatBufferBuilder(16);

			UpAndRunning.StartUpAndRunning(builder);
			var upAndRunningOffset = UpAndRunning.EndUpAndRunning(builder);

			Message.StartMessage(builder);
			Message.AddData(builder, upAndRunningOffset.Value);
			Message.AddDataType(builder, MessageDatum.UpAndRunning);

			var messageOffset = Message.EndMessage(builder);

			builder.Finish(messageOffset.Value);
			return builder.SizedByteArray();
		}

		private byte[] CreateWorkRequestRecievedMessage(int workloadID)
		{
			var builder = new FlatBufferBuilder(16);

			WorkloadReceived.StartWorkloadReceived(builder);
			WorkloadReceived.AddWorkloadID(builder, workloadID);
			var msgDataOffset = WorkloadReceived.EndWorkloadReceived(builder);

			Message.StartMessage(builder);
			Message.AddData(builder, msgDataOffset.Value);
			Message.AddDataType(builder, MessageDatum.WorkloadReceived);

			var msgOffset = Message.EndMessage(builder);

			builder.Finish(msgOffset.Value);
			return builder.SizedByteArray();
		}

		private BakeJob FilloutBakeJob(WorkloadRequest workloadRequest)
		{
			var bakeJob = new BakeJob();

			bakeJob.BounceCount				= workloadRequest.BounceCount;
			bakeJob.JobID					= workloadRequest.WorkloadID;

			var vec3 = workloadRequest.LightSourceForwardDir.Value;
			bakeJob.LightSourceForward		= new Vector4(vec3.X, vec3.Y, vec3.Z, 0.0f);

			vec3 = workloadRequest.LightSourceUpwardDir.Value;
			bakeJob.LightSourceUpward		= new Vector4(vec3.X, vec3.Y, vec3.Z, 0.0f);

			var vector3 = Vector3.Cross(bakeJob.LightSourceUpward, bakeJob.LightSourceForward);
			bakeJob.LightSourceRightward	= new Vector4(vector3.x, vector3.y, vector3.z, 0.0f);

			vec3 = workloadRequest.LightSourcePosition.Value;
			bakeJob.LightSourcePosition		= new Vector4(vec3.X, vec3.Y, vec3.Z, 1.0f);

			bakeJob.MaxRange				= workloadRequest.MaxRange;
			bakeJob.MinRange				= workloadRequest.MinRange;

			bakeJob.Resolution				= workloadRequest.Resolution;
			bakeJob.SampleCount				= workloadRequest.SampleCount;
			bakeJob.ShadowfocusPlane		= workloadRequest.ShadowFocusPlane;

			bakeJob.Indices					= workloadRequest.GetIndicesArray();

			var arraySize = workloadRequest.VerticesLength;
			bakeJob.Vertices = new Vector3[arraySize];
			for (int i = 0; i < arraySize; i++)
			{
				vec3 = workloadRequest.Vertices(i).Value;
				bakeJob.Vertices[i] = new Vector3(vec3.X, vec3.Y, vec3.Z);
			}

			arraySize = workloadRequest.ObjectDataLength;
			bakeJob.ObjectData = new ObjectMeshDatum[arraySize];
			for (int i = 0; i < arraySize; i++)
			{
				var ipc = workloadRequest.ObjectData(i).Value;

				var objectDatum = new ObjectMeshDatum();

				objectDatum.IndicesCount = ipc.IndicesCount;
				objectDatum.IndicesOffset = ipc.IndicesOffset;
				objectDatum.VerticesOffset = ipc.VerticesOffset;

				objectDatum.BoundingBox = new AABB_Bounds();

				var ipcBounds = ipc.Bounds;
				vec3 = ipcBounds.Center;
				objectDatum.BoundingBox.Center = new Vector3(vec3.X, vec3.Y, vec3.Z);
				vec3 = ipcBounds.Extent;
				objectDatum.BoundingBox.Extent = new Vector3(vec3.X, vec3.Y, vec3.Z);

				var ipcLTWM = ipc.LocalToWorldMatrix;
				objectDatum.LocalToWorldMatrix = new UnityEngine.Matrix4x4();

				objectDatum.LocalToWorldMatrix.SetRow(0, new Vector4(ipcLTWM.M00, ipcLTWM.M01, ipcLTWM.M02, ipcLTWM.M03));
				objectDatum.LocalToWorldMatrix.SetRow(1, new Vector4(ipcLTWM.M10, ipcLTWM.M11, ipcLTWM.M12, ipcLTWM.M13));
				objectDatum.LocalToWorldMatrix.SetRow(2, new Vector4(ipcLTWM.M20, ipcLTWM.M21, ipcLTWM.M22, ipcLTWM.M23));
				objectDatum.LocalToWorldMatrix.SetRow(3, new Vector4(ipcLTWM.M30, ipcLTWM.M31, ipcLTWM.M32, ipcLTWM.M33));

				bakeJob.ObjectData[i] = objectDatum;
			}

			return bakeJob;
		}

		private void ProcessWorkloadRequest(Message message)
		{
			var requestRawNullable = message.Data<WorkloadRequest>();
			
			// Pun intended
			// -FCT
			if (!requestRawNullable.HasValue)
				return;

			var requestRaw = requestRawNullable.Value;

			// Send a message back to the parent process to let it know that we have received this work request.
			_outgoingMessages.Enqueue(CreateWorkRequestRecievedMessage(requestRaw.WorkloadID));

			var bakeJob = FilloutBakeJob(requestRaw);

			throw new System.NotImplementedException();
		}

	}
}
