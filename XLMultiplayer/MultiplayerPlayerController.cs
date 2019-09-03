﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading;
using UnityModManagerNet;
using RootMotion.FinalIK;
using Harmony12;
using System.IO.Compression;

namespace XLMultiplayer {
	public enum MPTextureType : byte {
		Shirt = 0,
		Pants = 1,
		Shoes = 2, 
		Hat = 3,
		Board = 4
	}

	public class MultiplayerTexture {
		public byte[] bytes = null;
		public MPTextureType textureType;
		public Vector2 size;

		Texture2D texture;

		StreamWriter debugWriter;

		string file;
		public bool loaded = false;
		public bool saved = false;

		public MultiplayerTexture(byte[] b, Vector2 s, MPTextureType t, StreamWriter sw) {
			bytes = b;
			size = s;
			textureType = t;
			string path = Directory.GetCurrentDirectory() + "\\Mods\\XLMultiplayer\\Temp";
			if (!Directory.Exists(path)) {
				Directory.CreateDirectory(path);
			}
			File.WriteAllBytes(path + "\\" + t.ToString() + ".png", b);
			saved = true;
		}

		public MultiplayerTexture(StreamWriter sw, MPTextureType t) {
			this.debugWriter = sw;
			textureType = t;
		}

		public void LoadFromFileMainThread(MultiplayerPlayerController controller) {
			debugWriter.WriteLine("LOADING TEXTURE FROM MAIN THREAD");
			byte[] data = File.ReadAllBytes(file);
			texture = new Texture2D((int)size.x, (int)size.y);
			texture.LoadImage(data);
			controller.SetPlayerTexture(texture, textureType);
			loaded = true;
		}

		public void SaveTexture(int connectionId, byte[] buffer) {
			debugWriter.WriteLine("Saving texture in queue");
			size = new Vector2(BitConverter.ToSingle(buffer, 3), BitConverter.ToSingle(buffer, 7));
			byte[] file = new byte[buffer.Length - 11];
			Array.Copy(buffer, 11, file, 0, file.Length);

			if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\Mods\\XLMultiplayer\\Temp\\Clothing"))
				Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Mods\\XLMultiplayer\\Temp\\Clothing");

			File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\Mods\\XLMultiplayer\\Temp\\Clothing\\" + textureType.ToString() + connectionId.ToString() + ".png", file);

			this.file = Directory.GetCurrentDirectory() + "\\Mods\\XLMultiplayer\\Temp\\Clothing\\" + textureType.ToString() + connectionId.ToString() + ".png";
			saved = true;
			debugWriter.WriteLine("Saved texture in queue");
		}
	}

	public class MultiplayerPlayerController {
		public GameObject player { get; private set; }
		public GameObject skater { get; private set; }
		public GameObject board { get; private set; }

		public Animator animator { get; private set; }
		public Animator steezeAnimator { get; private set; }

		public int animBools { get; private set; }
		public int animFloats { get; private set; }
		public int animInts { get; private set; }

		public string[] animBoolNames { get; private set; }
		public string[] animFloatNames { get; private set; }
		public string[] animIntNames { get; private set; }

		public int animSteezeBools { get; private set; }
		public int animSteezeFloats { get; private set; }
		public int animSteezeInts { get; private set; }

		public string[] animSteezeBoolNames { get; private set; }
		public string[] animSteezeFloatNames { get; private set; }
		public string[] animSteezeIntNames { get; private set; }

		public Transform hips;

		public Vector3[] targetPositions = new Vector3[68];
		public Quaternion[] targetRotations = new Quaternion[68];

		public string username = "IT ALL BROKE";

		private GameObject usernameObject;
		private TextMesh usernameText;

		private StreamWriter debugWriter;

		public byte playerID;

		private int currentAnimationPacket = -1;
		private int currentPositionPacket = -1;
		readonly string[] SkateboardMaterials = new string[] { "GripTape", "Hanger", "Wheel1 Mesh", "Wheel2 Mesh", "Wheel3 Mesh", "Wheel4 Mesh" };
		readonly string[] TeeShirt = new string[] { "Cory_fixed_Karam:cory_001:shirt_geo" };
		readonly string[] PantsMaterials = new string[] { "Cory_fixed_Karam:cory_001:pants_geo" };
		readonly string[] Shoes = new string[] { "Cory_fixed_Karam:cory_001:shoes_geo" };
		readonly string[] Hat = new string[] { "Cory_fixed_Karam:cory_001:hat_geo" };

		public const string MainTextureName = "Texture2D_4128E5C7";

		Texture tShirtTexture;
		Texture pantsTexture;
		Texture shoesTexture;
		Texture hatTexture;
		Texture skateboardTexture;

		public MultiplayerTexture shirtMP;
		public MultiplayerTexture pantsMP;
		public MultiplayerTexture shoesMP;
		public MultiplayerTexture hatMP;
		public MultiplayerTexture boardMP;

		public bool copiedTextures = false;
		public bool startedEncoding = false;

		public bool loadedAll = false;

		public System.Collections.IEnumerator EncodeTextures() {
			if (!startedEncoding) {
				startedEncoding = true;
				Main.statusMenu.isLoading = true;
				Main.statusMenu.loadingStatus = 0;
				yield return new WaitForEndOfFrame();
				shirtMP = new MultiplayerTexture(ConvertTexture(tShirtTexture, MPTextureType.Shirt).EncodeToPNG(), new Vector2(tShirtTexture.width, tShirtTexture.height), MPTextureType.Shirt, debugWriter);
				Main.statusMenu.loadingStatus++;
				yield return new WaitForEndOfFrame();
				pantsMP = new MultiplayerTexture(ConvertTexture(pantsTexture, MPTextureType.Pants).EncodeToPNG(), new Vector2(pantsTexture.width, pantsTexture.height), MPTextureType.Pants, debugWriter);
				Main.statusMenu.loadingStatus++;
				yield return new WaitForEndOfFrame();
				shoesMP = new MultiplayerTexture(ConvertTexture(shoesTexture, MPTextureType.Shoes).EncodeToPNG(), new Vector2(shoesTexture.width, shoesTexture.height), MPTextureType.Shoes, debugWriter);
				Main.statusMenu.loadingStatus++;
				yield return new WaitForEndOfFrame();
				hatMP = new MultiplayerTexture(ConvertTexture(hatTexture, MPTextureType.Hat).EncodeToPNG(), new Vector2(hatTexture.width, hatTexture.height), MPTextureType.Hat, debugWriter);
				Main.statusMenu.loadingStatus++;
				yield return new WaitForEndOfFrame();
				boardMP = new MultiplayerTexture(ConvertTexture(skateboardTexture, MPTextureType.Board).EncodeToPNG(), new Vector2(skateboardTexture.width, skateboardTexture.height), MPTextureType.Board, debugWriter);
				copiedTextures = true;
				Main.statusMenu.loadingStatus++;
				yield return new WaitForEndOfFrame();

				Main.statusMenu.loadingStatus++;
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();
				Main.menu.multiplayerManager.SendTextures();
				yield return new WaitForEndOfFrame();

				Main.menu.multiplayerManager.InvokeRepeating("SendUpdate", 0.5f, 1.0f / (float)MultiplayerController.tickRate);
				yield return new WaitForEndOfFrame();
				Main.statusMenu.isLoading = false;
			}
			yield break;
		}

		private Texture2D ConvertTexture(Texture t, MPTextureType texType) {
			Texture2D texture2D = null;
			if (t.width <= 4096 && t.height <= 4096) {
				texture2D = new Texture2D(t.width, t.height, TextureFormat.RGB24, false);

				RenderTexture currentRT = RenderTexture.active;

				RenderTexture renderTexture = new RenderTexture(t.width, t.height, 32);
				Graphics.Blit(t, renderTexture);

				RenderTexture.active = renderTexture;
				texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
				texture2D.Apply();

				if (texture2D.width > 2048 || texture2D.height > 2048)
					TextureScale.Bilinear(texture2D, 2048, 2048);

				Color[] pixels = texture2D.GetPixels();

				RenderTexture.active = currentRT;
			} else {
				texture2D = new Texture2D(1024, 1024);
				byte[] textureData = File.ReadAllBytes(Directory.GetCurrentDirectory() + "\\Mods\\XLMultiplayer\\Clothing\\Default" + texType.ToString() + ".png");
				texture2D.LoadImage(textureData);
			}

			return texture2D;
		}

		public void SetPlayerTexture(Texture t, MPTextureType texType) {
			switch (texType) {
				case MPTextureType.Pants:
					foreach (Transform transform in this.skater.GetComponentsInChildren<Transform>()) {
						foreach (string s in PantsMaterials) {
							if (transform.name.Equals(s)) {
								transform.gameObject.GetComponent<Renderer>().material.SetTexture(MainTextureName, t);
							}
						}
					}
					break;
				case MPTextureType.Shirt:
					foreach (Transform tex in this.skater.GetComponentsInChildren<Transform>()) {
						foreach (string s in TeeShirt) {
							if (tex.name.Equals(s)) {
								tex.gameObject.GetComponent<Renderer>().material.SetTexture(MainTextureName, t);
							}
						}
					}
					break;
				case MPTextureType.Shoes:
					foreach (Transform tex in this.skater.GetComponentsInChildren<Transform>()) {
						foreach (string s in Shoes) {
							if (tex.name.Equals(s)) {
								tex.gameObject.GetComponent<Renderer>().material.SetTexture(MainTextureName, t);
							}
						}
					}
					break;
				case MPTextureType.Hat:
					foreach (Transform tex in this.skater.GetComponentsInChildren<Transform>()) {
						foreach (string s in Hat) {
							if (tex.name.Equals(s)) {
								tex.gameObject.GetComponent<Renderer>().material.SetTexture(MainTextureName, t);
							}
						}
					}
					break;
				case MPTextureType.Board:
					foreach (Transform tex in this.board.GetComponentsInChildren<Transform>()) {
						foreach (string s in SkateboardMaterials) {
							if (tex.name.Equals(s)) {
								tex.gameObject.GetComponent<Renderer>().material.SetTexture(MainTextureName, t);
							}
						}
					}
					break;
			}
		}

		public MultiplayerPlayerController(StreamWriter writer) {
			this.debugWriter = writer;
		}

		public void ConstructForPlayer() {
			//Write the master prefab hierarchy to make sure everything is in place
			StreamWriter writer = new StreamWriter("Hierarchy.txt");
			foreach (Transform t in GameObject.Find("Master Prefab").GetComponentsInChildren<Transform>()) {
				Transform parent = t.parent;
				while (parent != null) {
					writer.Write("\t");
					parent = parent.parent;
				}
				writer.WriteLine("└─>" + t.name + (t.GetComponents<Rigidbody>().Length != 0 ? "<Contains rigidbody>" : ""));
			}
			writer.Close();

			//Get the skater root gameobject and set it to the player
			this.player = PlayerController.Instance.skaterController.skaterTransform.gameObject;
			Transform[] componentsInChildren = PlayerController.Instance.gameObject.GetComponentsInChildren<Transform>();
			bool foundSkater = false;

			//Get the actual skater and skateboard from the root object
			for (int i = 0; i < componentsInChildren.Length; i++) {
				if (componentsInChildren[i].gameObject.name.Equals("Skater")) {
					if (!foundSkater) {
						if (componentsInChildren[i].Find("Steeze IK")) {
							this.skater = componentsInChildren[i].gameObject;
							this.debugWriter.WriteLine("Found Skater");
							foundSkater = true;
						}
					}
				} else if (componentsInChildren[i].gameObject.name.Equals("Skateboard")) {
					this.board = componentsInChildren[i].gameObject;
					this.debugWriter.WriteLine("Found Board");
					foreach(Transform t in componentsInChildren[i].GetComponentsInChildren<Transform>()) {
						if (t.name.Equals(SkateboardMaterials[0])) {
							skateboardTexture = t.GetComponent<Renderer>().material.GetTexture(MainTextureName);
							break;
						}
					}
				} else if (componentsInChildren[i].name.Equals(PantsMaterials[0])) {
					pantsTexture = componentsInChildren[i].GetComponent<Renderer>().material.GetTexture(MainTextureName);
				} else if (componentsInChildren[i].name.Equals(TeeShirt[0])) {
					tShirtTexture = componentsInChildren[i].GetComponent<Renderer>().material.GetTexture(MainTextureName);
				} else if (componentsInChildren[i].name.Equals(Shoes[0])) {
					shoesTexture = componentsInChildren[i].GetComponent<Renderer>().material.GetTexture(MainTextureName);
				} else if (componentsInChildren[i].name.Equals(Hat[0])) {
					hatTexture = componentsInChildren[i].GetComponent<Renderer>().material.GetTexture(MainTextureName);
				}
			}

			if (!foundSkater) {
				this.debugWriter.WriteLine("Failed to find skater");
				return;
			}

			this.hips = Traverse.Create(PlayerController.Instance.ikController).Field("_finalIk").GetValue<FullBodyBipedIK>().references.pelvis;

			//Get all animators attached to the root
			Animator[] ourSkaterAnimators = new Animator[3];
			Array.Copy(this.skater.GetComponentsInChildren<Animator>(), ourSkaterAnimators, 2);
			ourSkaterAnimators[2] = PlayerController.Instance.animationController.ikAnim;

			if (ourSkaterAnimators[0] == null || ourSkaterAnimators[1] == null || ourSkaterAnimators[2] == null) {
				this.debugWriter.WriteLine("Failed to find an animator {0}, {1}, {2}", ourSkaterAnimators[0] == null, ourSkaterAnimators[1] == null, ourSkaterAnimators[2] == null);
				return;
			}

			//Set our animator and steeze animator
			this.animator = ourSkaterAnimators[0];
			this.steezeAnimator = ourSkaterAnimators[1];

			//Get the paramater names, types, and amount of each type from main animator and log all others
			this.animBools = 0;
			this.animFloats = 0;
			this.animInts = 0;
			for (int i = 0; i < ourSkaterAnimators.Length; i++) {
				debugWriter.WriteLine("Animator {0}: {1}, humanoid is {2}", i, ourSkaterAnimators[i].name, ourSkaterAnimators[i].isHuman);
				List<string> boolParams = new List<string>();
				List<string> floatParams = new List<string>();
				List<string> intParams = new List<string>();
				foreach (AnimatorControllerParameter param in ourSkaterAnimators[i].parameters) {
					if (param.type == AnimatorControllerParameterType.Bool) {
						if (i == 0)
							this.animBools++;
						else if (i == 1)
							this.animSteezeBools++;
						boolParams.Add(param.name);
					} else if (param.type == AnimatorControllerParameterType.Float) {
						if (i == 0)
							this.animFloats++;
						else if (i == 1)
							this.animSteezeFloats++;
						floatParams.Add(param.name);
					} else if (param.type == AnimatorControllerParameterType.Int) {
						if (i == 0)
							this.animInts++;
						else if (i == 1)
							this.animSteezeInts++;
						intParams.Add(param.name);
					}
				}
				debugWriter.Write("\tBoolean Paramaters: ");
				for (int c = 0; c < boolParams.ToArray().Length; c++)
					debugWriter.Write("\"{0}\", ", boolParams[c]);
				debugWriter.Write("\n\tFloat Paramaters: ");
				for (int c = 0; c < floatParams.ToArray().Length; c++)
					debugWriter.Write("\"{0}\", ", floatParams[c]);
				debugWriter.Write("\n\tInteger Paramaters: ");
				for (int c = 0; c < intParams.ToArray().Length; c++)
					debugWriter.Write("\"{0}\", ", intParams[c]);
				debugWriter.Write("\n");

				if (i == 0) {
					this.animBoolNames = boolParams.ToArray();
					this.animFloatNames = floatParams.ToArray();
					this.animIntNames = intParams.ToArray();
				} else if (i == 1) {
					this.animSteezeBoolNames = boolParams.ToArray();
					this.animSteezeFloatNames = floatParams.ToArray();
					this.animSteezeIntNames = intParams.ToArray();
				}

				boolParams.Clear();
				floatParams.Clear();
				intParams.Clear();
			}
		}

		public void ConstructFromPlayer(MultiplayerPlayerController source) {
			//Create a new root object for the player
			this.player = new GameObject();
			UnityEngine.Object.DontDestroyOnLoad(this.player);
			this.player.name = "New Player";
			this.player.transform.SetParent(null);
			this.player.transform.position = PlayerController.Instance.transform.position;
			debugWriter.WriteLine("Created New Player");

			Time.timeScale = 0.0f;
			foreach (MonoBehaviour m in source.skater.GetComponentsInChildren<MonoBehaviour>()) {
				m.enabled = false;
			}
			foreach (MonoBehaviour m in source.board.GetComponentsInChildren<MonoBehaviour>()) {
				m.enabled = false;
			}

			//Copy board from the source and reparent/rename it for the new player and remove all scripts
			//All scripts in the game use PlayerController.Instance and end up breaking the original character if left in
			//I'm also too lazy to convert every script to be multiplayer compatible hence why client state is just being copied
			this.board = UnityEngine.Object.Instantiate<GameObject>(source.board, this.player.transform, false);
			this.board.name = "New Player Board";
			this.board.transform.localPosition = Vector3.zero;
			debugWriter.WriteLine("Created New Board");
			foreach (MonoBehaviour m in this.board.GetComponentsInChildren<MonoBehaviour>()) {
				m.enabled = false;
				debugWriter.WriteLine("Removing script from additional board");
				UnityEngine.Object.DestroyImmediate(m);
			}

			//Copy the source players skater for our new player
			this.skater = UnityEngine.Object.Instantiate<GameObject>(source.skater, this.player.transform, false);
			this.skater.name = "New Player Skater";
			this.skater.transform.localPosition = Vector3.zero;
			debugWriter.WriteLine("Created New Skater");
			foreach (MonoBehaviour m in this.skater.GetComponentsInChildren<MonoBehaviour>()) {
				m.enabled = false;
				debugWriter.WriteLine("Removing script from additional skater");
				UnityEngine.Object.DestroyImmediate(m);
			}

			this.hips = this.skater.transform.Find("Skater").Find("Reference").Find("mixamorig_Hips");

			foreach (MonoBehaviour m in source.skater.GetComponentsInChildren<MonoBehaviour>()) {
				m.enabled = true;
			}
			foreach (MonoBehaviour m in source.board.GetComponentsInChildren<MonoBehaviour>()) {
				m.enabled = true;
			}
			Time.timeScale = 1.0f;

			this.animBools = source.animBools;
			this.animFloats = source.animFloats;
			this.animInts = source.animInts;

			this.animBoolNames = source.animBoolNames;
			this.animFloatNames = source.animFloatNames;
			this.animIntNames = source.animIntNames;

			this.animSteezeBools = source.animSteezeBools;
			this.animSteezeFloats = source.animSteezeFloats;
			this.animSteezeInts = source.animSteezeInts;

			this.animSteezeBoolNames = source.animSteezeBoolNames;
			this.animSteezeFloatNames = source.animSteezeFloatNames;
			this.animSteezeIntNames = source.animSteezeIntNames;
			debugWriter.WriteLine("Set New Player Animation variables");

			//Get the animators on the new player
			Animator[] newSkaterAnimators = this.skater.GetComponentsInChildren<Animator>();
			this.animator = newSkaterAnimators[0];
			this.animator.enabled = true;
			this.steezeAnimator = newSkaterAnimators[1];
			newSkaterAnimators[1].enabled = true;
			debugWriter.WriteLine("Activated New Player Animators");

			this.animator.enabled = false;
			this.steezeAnimator.enabled = false;

			this.usernameObject = new GameObject("Username Object");
			this.usernameObject.transform.SetParent(this.player.transform, false);
			this.usernameObject.transform.localScale = new Vector3(-0.01f, 0.01f, 1f);
			this.usernameObject.AddComponent<MeshRenderer>();
			this.usernameObject.GetComponent<MeshRenderer>().material = Resources.FindObjectsOfTypeAll<Font>()[0].material;
			this.usernameText = this.usernameObject.AddComponent<TextMesh>();
			this.usernameText.text = username;
			this.usernameText.fontSize = 256;
			this.usernameText.font = Resources.FindObjectsOfTypeAll<Font>()[0];
			this.usernameText.color = Color.black;
			this.usernameText.alignment = TextAlignment.Center;

			this.shirtMP = new MultiplayerTexture(this.debugWriter, MPTextureType.Shirt);
			this.pantsMP = new MultiplayerTexture(this.debugWriter, MPTextureType.Pants);
			this.hatMP = new MultiplayerTexture(this.debugWriter, MPTextureType.Hat);
			this.shoesMP = new MultiplayerTexture(this.debugWriter, MPTextureType.Shoes);
			this.boardMP = new MultiplayerTexture(this.debugWriter, MPTextureType.Board);
		}

		public byte[] PackTransforms() {
			Transform[] T = new Transform[] { this.player.transform, this.board.transform, this.skater.transform };
			Rigidbody[] R = new Rigidbody[] { this.board.GetComponent<Rigidbody>(), this.board.GetComponent<Rigidbody>(), this.board.GetComponentsInChildren<Rigidbody>()[1], this.board.GetComponentsInChildren<Rigidbody>()[2] };

			byte[] packed = new byte[T.Length * 28 + R.Length * 40];
			byte[] TPacked = this.PackTransformArray(T);
			Array.Copy(TPacked, 0, packed, 0, TPacked.Length);

			for (int i = 0; i < R.Length; i++) {
				Array.Copy(BitConverter.GetBytes(R[i].position.x), 0, packed, T.Length * 28 + i * 40, 4);
				Array.Copy(BitConverter.GetBytes(R[i].position.y), 0, packed, T.Length * 28 + i * 40 + 4, 4);
				Array.Copy(BitConverter.GetBytes(R[i].position.z), 0, packed, T.Length * 28 + i * 40 + 8, 4);
				Array.Copy(BitConverter.GetBytes(R[i].rotation.x), 0, packed, T.Length * 28 + i * 40 + 12, 4);
				Array.Copy(BitConverter.GetBytes(R[i].rotation.y), 0, packed, T.Length * 28 + i * 40 + 16, 4);
				Array.Copy(BitConverter.GetBytes(R[i].rotation.z), 0, packed, T.Length * 28 + i * 40 + 20, 4);
				Array.Copy(BitConverter.GetBytes(R[i].rotation.w), 0, packed, T.Length * 28 + i * 40 + 24, 4);
				Array.Copy(BitConverter.GetBytes(R[i].velocity.x), 0, packed, T.Length * 28 + i * 40 + 28, 4);
				Array.Copy(BitConverter.GetBytes(R[i].velocity.y), 0, packed, T.Length * 28 + i * 40 + 32, 4);
				Array.Copy(BitConverter.GetBytes(R[i].velocity.z), 0, packed, T.Length * 28 + i * 40 + 36, 4);
			}

			return packed;
		}

		int outoforder = 0;
		int total = 0;

		public void UnpackTransforms(byte[] recBuffer) {
			int receivedPacketSequence = BitConverter.ToInt32(recBuffer, 0);
			total++;
			byte[] buffer = new byte[recBuffer.Length - 4];
			if (receivedPacketSequence < currentPositionPacket) {
				outoforder++;
				return;
			} else {
				currentPositionPacket = receivedPacketSequence;
				Array.Copy(recBuffer, 4, buffer, 0, recBuffer.Length - 4);
			}

			List<Vector3> vectors = new List<Vector3>();
			List<Quaternion> quaternions = new List<Quaternion>();

			for (int i = 0; i < 3; i++) {
				Vector3 readVector = new Vector3();
				readVector.x = BitConverter.ToSingle(buffer, i * 28);
				readVector.y = BitConverter.ToSingle(buffer, i * 28 + 4);
				readVector.z = BitConverter.ToSingle(buffer, i * 28 + 8);
				Quaternion readQuaternion = new Quaternion();
				readQuaternion.x = BitConverter.ToSingle(buffer, i * 28 + 12);
				readQuaternion.y = BitConverter.ToSingle(buffer, i * 28 + 16);
				readQuaternion.z = BitConverter.ToSingle(buffer, i * 28 + 20);
				readQuaternion.w = BitConverter.ToSingle(buffer, i * 28 + 24);

				vectors.Add(readVector);
				quaternions.Add(readQuaternion);
			}

			for (int i = 0; i < (buffer.Length - 28 * 3) / 40; i++) {
				Vector3 readVector = new Vector3();
				readVector.x = BitConverter.ToSingle(buffer, i * 40 + 28 * 3);
				readVector.y = BitConverter.ToSingle(buffer, i * 40 + 4 + 28 * 3);
				readVector.z = BitConverter.ToSingle(buffer, i * 40 + 8 + 28 * 3);
				vectors.Add(readVector);

				Quaternion readQuaternion = new Quaternion();
				readQuaternion.x = BitConverter.ToSingle(buffer, i * 40 + 12 + 28 * 3);
				readQuaternion.y = BitConverter.ToSingle(buffer, i * 40 + 16 + 28 * 3);
				readQuaternion.z = BitConverter.ToSingle(buffer, i * 40 + 20 + 28 * 3);
				readQuaternion.w = BitConverter.ToSingle(buffer, i * 40 + 24 + 28 * 3);
				quaternions.Add(readQuaternion);

				readVector = new Vector3();
				readVector.x = BitConverter.ToSingle(buffer, i * 40 + 28 + 28 * 3);
				readVector.y = BitConverter.ToSingle(buffer, i * 40 + 32 + 28 * 3);
				readVector.z = BitConverter.ToSingle(buffer, i * 40 + 36 + 28 * 3);
				vectors.Add(readVector);
			}

			SetTransforms(vectors.ToArray(), quaternions.ToArray());
		}

		public void SetTransforms(Vector3[] vectors, Quaternion[] quaternions) {
			this.player.transform.position = vectors[0];
			this.player.transform.rotation = quaternions[0];
			this.board.transform.position = vectors[1];
			this.board.transform.rotation = quaternions[1];
			this.skater.transform.position = vectors[2];
			this.skater.transform.rotation = quaternions[2];
			//this.skater.GetComponent<Rigidbody>().position = vectors[3];
			//this.skater.GetComponent<Rigidbody>().velocity = vectors[4];
			//this.skater.GetComponent<Rigidbody>().rotation = quaternions[3];
			Rigidbody[] boardBodies = this.board.GetComponentsInChildren<Rigidbody>();
			boardBodies[0].position = vectors[5];
			boardBodies[0].velocity = vectors[6];
			boardBodies[0].rotation = quaternions[4];
			boardBodies[1].position = vectors[7];
			boardBodies[1].velocity = vectors[8];
			boardBodies[1].rotation = quaternions[5];
			boardBodies[2].position = vectors[9];
			boardBodies[2].velocity = vectors[10];
			boardBodies[2].rotation = quaternions[6];

			this.usernameText.text = this.username;
			this.usernameObject.transform.position = this.player.transform.position + this.player.transform.up;
			this.usernameObject.transform.LookAt(Camera.main.transform);
		}

		public byte[] PackTransformArray(Transform[] T) {
			byte[] packed = new byte[T.Length * 28];
			for (int i = 0; i < T.Length; i++) {
				Array.Copy(BitConverter.GetBytes(T[i].position.x), 0, packed, i * 28, 4);
				Array.Copy(BitConverter.GetBytes(T[i].position.y), 0, packed, i * 28 + 4, 4);
				Array.Copy(BitConverter.GetBytes(T[i].position.z), 0, packed, i * 28 + 8, 4);
				Array.Copy(BitConverter.GetBytes(T[i].rotation.x), 0, packed, i * 28 + 12, 4);
				Array.Copy(BitConverter.GetBytes(T[i].rotation.y), 0, packed, i * 28 + 16, 4);
				Array.Copy(BitConverter.GetBytes(T[i].rotation.z), 0, packed, i * 28 + 20, 4);
				Array.Copy(BitConverter.GetBytes(T[i].rotation.w), 0, packed, i * 28 + 24, 4);
			}
			return packed;
		}

		public byte[][] PackAnimator() {
			byte[][] packed = new byte[2][];

			byte[] transforms = PackTransformArray(this.hips.GetComponentsInChildren<Transform>());

			packed[0] = new byte[953];
			packed[0][0] = 0;
			Array.Copy(transforms, 0, packed[0], 1, 952);

			packed[1] = new byte[953];
			packed[1][0] = 1;
			Array.Copy(transforms, 952, packed[1], 1, 952);

			return packed;
		}

		public void UnpackAnimator(byte[] recBuffer) {
			int receivedPacketSequence = BitConverter.ToInt32(recBuffer, 0);

			byte[] buffer = new byte[recBuffer.Length - 5];
			if (receivedPacketSequence < currentAnimationPacket - 1) {
				return;
			} else {
				Array.Copy(recBuffer, 5, buffer, 0, recBuffer.Length - 5);
				currentAnimationPacket = receivedPacketSequence;
			}

			List<Vector3> vectors = new List<Vector3>();
			List<Quaternion> quaternions = new List<Quaternion>();

			for (int i = 0; i < 34; i++) {
				Vector3 readVector = new Vector3();
				readVector.x = BitConverter.ToSingle(buffer, i * 28);
				readVector.y = BitConverter.ToSingle(buffer, i * 28 + 4);
				readVector.z = BitConverter.ToSingle(buffer, i * 28 + 8);
				Quaternion readQuaternion = new Quaternion();
				readQuaternion.x = BitConverter.ToSingle(buffer, i * 28 + 12);
				readQuaternion.y = BitConverter.ToSingle(buffer, i * 28 + 16);
				readQuaternion.z = BitConverter.ToSingle(buffer, i * 28 + 20);
				readQuaternion.w = BitConverter.ToSingle(buffer, i * 28 + 24);

				vectors.Add(readVector);
				quaternions.Add(readQuaternion);
			}

			if (recBuffer[4] == 0) {
				for (int i = 0; i < 34; i++) {
					if (Vector3.Distance(this.targetPositions[i], vectors[i]) > 1) {
						this.hips.GetComponentsInChildren<Transform>()[i].position = vectors[i];
						this.hips.GetComponentsInChildren<Transform>()[i].rotation = quaternions[i];
					} else {
						this.hips.GetComponentsInChildren<Transform>()[i].position = this.targetPositions[i];
						this.hips.GetComponentsInChildren<Transform>()[i].rotation = this.targetRotations[i];
					}
					this.targetPositions[i] = vectors[i];
					this.targetRotations[i] = quaternions[i];
				}
			} else {
				for (int i = 34; i < 68; i++) {
					if (Vector3.Distance(this.targetPositions[i], vectors[i - 34]) > 1) {
						this.hips.GetComponentsInChildren<Transform>()[i].position = vectors[i - 34];
						this.hips.GetComponentsInChildren<Transform>()[i].rotation = quaternions[i - 34];
					} else {
						this.hips.GetComponentsInChildren<Transform>()[i].position = this.targetPositions[i];
						this.hips.GetComponentsInChildren<Transform>()[i].rotation = this.targetRotations[i];
					}
					this.targetPositions[i] = vectors[i - 34];
					this.targetRotations[i] = quaternions[i - 34];
				}
			}
		}
	}
}