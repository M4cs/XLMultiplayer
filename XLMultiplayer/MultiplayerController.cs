using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Jobs;
using Unity.Collections;
using System.Text;

namespace XLMultiplayer {
	public class MultiplayerController : MonoBehaviour {
		private void Start() {
		}

		private void Update() {
			this.UpdateClient();
		}

		private void SendUpdate() {
			this.SendPlayerPosition();
			this.SendPlayerAnimator();
		}

		public void ConnectToServer(string serverIP, int port, string user) {
			if (!this.runningClient && !this.runningServer) {
				int i = 0;
				while (this.debugWriter == null) {
					string filename = "Multiplayer Debug Client" + (i == 0 ? "" : " " + i.ToString()) + ".txt";
					try {
						this.debugWriter = new StreamWriter(filename);
					} catch (Exception e) {
						this.debugWriter = null;
						i++;
					}
				}
				this.debugWriter.AutoFlush = true;
				this.debugWriter.WriteLine("Attempting to connect to server ip {0} on port {1}", serverIP, port.ToString());
				NetworkTransport.Init();
				ConnectionConfig connectionConfig = new ConnectionConfig();
				connectionConfig.PacketSize = 1400;
				this.reliableChannel = connectionConfig.AddChannel(QosType.Reliable);
				HostTopology topology = new HostTopology(connectionConfig, 2);
				this.hostId = NetworkTransport.AddHost(topology);
				if (this.hostId < 0) {
					this.debugWriter.WriteLine("Failed socket creation for client");
					NetworkTransport.Shutdown();
					return;
				} else {
					this.debugWriter.WriteLine("Successfully created client socket");
				}
				this.connectionId = NetworkTransport.Connect(this.hostId, serverIP, port, 0, out this.error);
				if (this.error != 0) {
					this.debugWriter.WriteLine((NetworkError)this.error);
					return;
				}
				this.ourController = new MultiplayerPlayerController(debugWriter);
				this.ourController.ConstructForPlayer();
				this.ourController.username = user;
				this.runningClient = true;
			}
		}

		public void KillServer() {
			this.runningServer = false;
			NetworkTransport.Shutdown();
		}

		private void UpdateClient() {
			byte[] buffer = new byte[1024];
			int hId;
			int conId;
			int chanId;
			int bufSize;
			NetworkEventType networkEvent = NetworkTransport.Receive(out hId, out conId, out chanId, buffer, 1024, out bufSize, out this.error);
			while (networkEvent != NetworkEventType.Nothing) {
				if (this.error != (int)NetworkError.Ok)
					debugWriter.WriteLine("Error recieving message {0}", this.error);
				switch (networkEvent) {
					case NetworkEventType.ConnectEvent:
						debugWriter.WriteLine("Successfully connected to server");
						this.SendBytes(2, Encoding.ASCII.GetBytes(this.ourController.username));
						InvokeRepeating("SendUpdate", 0.5f, 1.0f / (float)tickRate);
						break;
					case NetworkEventType.DataEvent:
						ProcessMessage(buffer, bufSize);
						break;
					case NetworkEventType.DisconnectEvent:
						this.debugWriter.WriteLine("Connection to server ended");
						this.KillConnection();
						break;
				}
				networkEvent = NetworkTransport.Receive(out hId, out conId, out chanId, buffer, 1024, out bufSize, out this.error);
			}
		}

		private void AddPlayer(int playerID) {
			MultiplayerPlayerController newController = new MultiplayerPlayerController(debugWriter);
			newController.ConstructFromPlayer(this.ourController);
			newController.playerID = playerID;
			otherControllers.Add(newController);
		}

		private void RemovePlayer(int playerID) {
			int index = -1;
			for (int i = 0; i < otherControllers.Count; i++) {
				if (otherControllers[i].playerID == playerID) {
					index = i;
					break;
				}
			}
			if (index != -1) {
				MultiplayerPlayerController controller = otherControllers[index];
				Destroy(controller.player);
				otherControllers.RemoveAt(index);
			}
		}

		private void SendPlayerPosition() {
			this.SendBytes(0, this.ourController.PackTransforms());
		}

		private void SendPlayerAnimator() {
			this.SendBytes(1, this.ourController.PackAnimator());
		}

		public void KillConnection() {
			this.runningClient = false;
			CancelInvoke("SendUpdate");
			List<int> players = new List<int>();
			foreach (MultiplayerPlayerController connection in otherControllers) {
				players.Add(connection.playerID);
			}
			foreach (int i in players) {
				RemovePlayer(i);
			}
			NetworkTransport.Disconnect(this.hostId, this.connectionId, out this.error);
			NetworkTransport.Shutdown();
		}

		private void ProcessMessage(byte[] buffer, int bufferSize) {
			byte[] newBuffer = new byte[bufferSize - 5];
			Array.Copy(buffer, 1, newBuffer, 0, bufferSize - 5);

			byte opCode = buffer[0];
			int playerID = BitConverter.ToInt32(buffer, bufferSize - 4);

			this.debugWriter.WriteLine("Message: {0} {1}", opCode, playerID);

			switch (opCode) {
				case 0:
					foreach (MultiplayerPlayerController controller in otherControllers) {
						if (controller.playerID == playerID)
							controller.UnpackTransforms(newBuffer);
					}
					break;
				case 1:
					foreach (MultiplayerPlayerController controller in otherControllers) {
						if (controller.playerID == playerID)
							controller.UnpackAnimator(newBuffer);
					}
					break;
				case 2:
					foreach (MultiplayerPlayerController controller in otherControllers) {
						if (controller.playerID == playerID) {
							controller.username = Encoding.ASCII.GetString(newBuffer, 0, bufferSize - 5);
							debugWriter.WriteLine(controller.username);
						}
					}
					break;
				case 254:
					this.AddPlayer(playerID);
					break;
				case 255:
					this.RemovePlayer(playerID);
					break;
			}
		}

		public void OnDestroy() {
		}

		public void OnApplicationQuit() {
		}

		private void SendBytes(byte opCode, byte[] msg) {
			if (this.connectionId != 0) {
				byte[] buffer = new byte[msg.Length + 1];
				buffer[0] = opCode;
				Array.Copy(msg, 0, buffer, 1, msg.Length);
				NetworkTransport.Send(this.hostId, this.connectionId, (int)this.reliableChannel, buffer, buffer.Length, out this.error);
				if (this.error != 0) {
					this.debugWriter.WriteLine((NetworkError)this.error);
				}
			}
		}

		public bool runningServer = false;
		public bool runningClient = false;

		private byte tickRate = 32;

		public MultiplayerPlayerController ourController;
		public List<MultiplayerPlayerController> otherControllers = new List<MultiplayerPlayerController>();

		private int hostId;
		private int connectionId;
		private byte reliableChannel;
		private byte error;

		private StreamWriter debugWriter;
	}
}