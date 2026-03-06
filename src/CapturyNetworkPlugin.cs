using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net;

namespace Captury
{
	//==========================================
	// internal structures that are more easy to use
	//==========================================
	[Serializable]
	public class CapturySkeletonJoint
	{
		public string name;
		public int parent;
		public Vector3 offset;
		public Vector3 orientation;
		public Vector3 scale;
		public Matrix transform;
		public Quaternion originalRotation;
	}

	[Serializable]
	public class CapturySkeleton
	{
		public string name;
		public int id;
		public CapturySkeletonJoint[] joints; // joints that are streamed
		public string[] blendShapeNames;
		public float[] blendShapeActivations;

		public int scalingProgress = 100; // [0 .. 100]
		public int trackingQuality = 100; // [0 .. 100]

		public static CapturyNetworkPlugin networkPlugin;

		public float streamedBackLength = -1.0f;
		public float targetBackLength = -1.0f;

		public Dictionary<CapturyNetworkPlugin.PhysiologicalAngleType, float> angles = new Dictionary<CapturyNetworkPlugin.PhysiologicalAngleType, float>();

		// reference to game object that gets animated
		// set it with setTargetSkeleton()
		//public GameObject Target
		//{
		//	get { return targetSkeleton; }
		//}

		// reference to game object that the motion is applied to directly
		// set it with setReferenceSkeleton()
		//public GameObject Reference
		//{
		//	get { return referenceSkeleton; }
		//}

		public bool isLeftFootOnGround = false;
		public bool isRightFootOnGround = false;

		// note: CapturySkeleton takes ownership of the passed skeleton and destroys it when it is destroyed
		//		public void SetReferenceSkeleton(GameObject refSkel, Avatar avatar, float backLen)
		//		{
		//			if (referenceSkeleton != null)
		//			{
		//				networkPlugin.LogWarning("CapturySkeleton.setReferenceSkeleton() can only be called once");
		//				return;
		//			}

		//			referenceSkeleton = refSkel;
		//			referenceName = referenceSkeleton.name;

		//			poseGetter = new HumanPoseHandler(avatar, refSkel.transform);
		//			foreach (CapturySkeletonJoint j in joints)
		//			{
		//				// check if the joint name matches a reference transform and assign it
		//				ArrayList children = referenceSkeleton.transform.GetAllChildren();
		//				foreach (Transform tra in children)
		//				{
		//					if (tra.name.EndsWith(j.name))
		//					{
		//						j.transform = tra;
		//						j.originalRotation = tra.rotation;
		//						continue;
		//					}
		//				}
		//			}

		//			lock (this)
		//			{
		//				streamedBackLength = backLen;

		//				if (targetSkeleton && targetBackLength > 0.0f && streamedBackLength > 0.0f)
		//				{
		//					float scale = streamedBackLength / targetBackLength;
		//					networkPlugin.Log("Captury: SCALING2 avatar " + id + " by " + scale, refSkel);
		//					targetSkeleton.transform.localScale = new Vector3(scale, scale, scale);
		//				}
		//			}
		//		}

		//		public void SetTargetSkeleton(GameObject targetSkel, Avatar avatar, float avatarBackLength)
		//		{
		//			targetSkeleton = targetSkel;
		//			if (targetSkeleton == null)
		//			{
		//				targetName = "";
		//				return;
		//			}
		//			targetName = targetSkeleton.name;

		//			if (joints.Length == 1)
		//			{ // rigid object
		//				joints[0].transform = targetSkeleton.transform;
		//				return;
		//			}

		//			// scale skeleton to size of actor
		//			lock (this)
		//			{
		//				targetBackLength = avatarBackLength;
		//				if (targetSkeleton)
		//				{
		//					if (targetBackLength > 0.0f && streamedBackLength > 0.0f)
		//					{
		//						float scale = streamedBackLength / targetBackLength;
		//						networkPlugin.Log("Captury: SCALING1 avatar " + id + " by " + scale, targetSkel);
		//						targetSkeleton.transform.localScale = new Vector3(scale, scale, scale);
		//					}
		//					else
		//					{
		//						networkPlugin.Log("Captury: NOT SCALING avatar " + id + " streamed " + streamedBackLength + " avatar " + avatarBackLength, targetSkel);
		//					}

		//					try
		//					{
		//						poseSetter = new HumanPoseHandler(avatar, targetSkeleton.transform);
		//					}
		//					catch
		//					{
		//						networkPlugin.LogError("Captury: the assigned target avatar is not valid. Please make sure the avatar passed to CapturyNetworkPlugin.setTargetSkeleton() is a valid humanoid avatar.", targetSkel);
		//#if UNITY_EDITOR
		//						UnityEditor.EditorApplication.isPlaying = false;
		//#endif
		//					}
		//				}
		//			}
		//		}

		//the pose has been set on the referenceSkeleton already
		//now apply it on the target skeleton
		//public void UpdatePose()
		//{
		//	if (targetSkeleton == null || referenceSkeleton == null || poseGetter == null || poseSetter == null)
		//		return;

		//	HumanPose pose = new HumanPose();
		//	poseGetter.GetHumanPose(ref pose);
		//	poseSetter.SetHumanPose(ref pose);

		//	if (blendShapeActivations.Length != 0)
		//	{
		//		SkinnedMeshRenderer[] renderers = targetSkeleton.GetComponentsInChildren<SkinnedMeshRenderer>();
		//		foreach (SkinnedMeshRenderer renderer in renderers)
		//		{
		//			for (int i = 0; i < Math.Min(blendShapeActivations.Length, renderer.sharedMesh.blendShapeCount); ++i)
		//				renderer.SetBlendShapeWeight(i, blendShapeActivations[i] * 100);
		//		}
		//	}
		//}

		public void UpdateAngles(CapturyAngleData[] angleData)
		{
			const float RAD2DEGf = 57.29577951308232088f;
			lock (angles)
			{
				foreach (CapturyAngleData angle in angleData)
				{
					angles[(CapturyNetworkPlugin.PhysiologicalAngleType)angle.type] = angle.value * RAD2DEGf;
				}
			}
		}

		public void Deactivate()
		{
			//Debug.LogWarning($"CapturySkeleton.Deactivate(id {id}), preparing to removing a skeleton!");
			//referenceSkeleton.SetActive(false);
			//referenceSkeletonsToDestroy.Enqueue(referenceSkeleton);
			////lock (referenceSkeletonsToDestroy) {
			////	referenceSkeletonsToDestroy.Add(referenceSkeleton);
			////}
			//referenceSkeleton = null;
		}

		public static void Cleanup()
		{
			//lock (referenceSkeletonsToDestroy) {
			//while (referenceSkeletonsToDestroy.TryDequeue(out var skel))
			{
				//Debug.LogWarning($"CapturySkeleton.Cleanup(), removing a referenceSkeleton!");
				//if (skel != null)
				//	UnityEngine.Object.Destroy(skel);
			}
			//foreach (GameObject skel in referenceSkeletonsToDestroy)
			//	UnityEngine.Object.Destroy(skel);
			//referenceSkeletonsToDestroy.Clear();
			//}
		}

		~CapturySkeleton()
		{
			//Debug.LogWarning($"~CapturySkeleton(id {id}) (destructor)");
			//if (referenceSkeleton) {
			//    //lock (referenceSkeletonsToDestroy) {
			//    //referenceSkeleton.SetActive(false);
			//    referenceSkeletonsToDestroy.Enqueue(referenceSkeleton);
			//    //}
			//}
		}

		//private static readonly ConcurrentQueue<GameObject> referenceSkeletonsToDestroy = new ConcurrentQueue<GameObject>();

		//private GameObject referenceSkeleton = null; // the reference skeleton
		public String referenceName;
		//private HumanPoseHandler poseGetter;
		//private GameObject targetSkeleton = null; // the target skeleton
		public String targetName;
		//private HumanPoseHandler poseSetter;
	}

	//[Serializable]
	public class CapturyMarkerTransform
	{
		public Quaternion rotation;
		public Vector3 translation;
		public Int64 timestamp;
		public float bestAccuracy;
		public bool consumed;
	}

	[Serializable]
	public class ARTag
	{
		public int id;
		public Vector3 translation;
		public Quaternion rotation;
	}

	//=================================
	// define captury class structures
	//=================================
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct CapturyJoint
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
		public byte[] name;
		public int parent;
		public float ox, oy, oz;
		public float rx, ry, rz;
		public float sx, sy, sz;
		public byte boneType;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct CapturyActor
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] name;
		public int id;
		public int numJoints;
		public IntPtr joints;
		public int numBlobs;
		public IntPtr blobs;
		public int numBlendShapes;
		public IntPtr blendShapes;
		public int numMetaData;
		public IntPtr metaDataKeys;    // metaDataKeys[i] -> metaDataValues[i]
		public IntPtr metaDataValues;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct CapturyPose
	{
		public Int32 actor;
		public Int64 timestamp;
		public Int32 numTransforms;
		public IntPtr transforms;
		public Int32 flags;
		public Int32 numBlendShapes;
		public IntPtr blendShapeActivations;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct CapturyARTag
	{
		public int id;
		public float ox, oy, oz; // position
		public float nx, ny, nz; // normal
	}

	[StructLayout(LayoutKind.Sequential)]
	struct CapturyImage
	{
		public int width;
		public int height;
		public int camera;
		public ulong timestamp;
		public IntPtr data;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct CapturyTransform
	{
		public float rx; // rotation euler angles
		public float ry;
		public float rz;
		public float tx; // translation
		public float ty;
		public float tz;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	unsafe struct CapturyCamera
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] name;
		public int id;
		public float positionX;
		public float positionY;
		public float positionZ;
		public float orientationX;
		public float orientationY;
		public float orientationZ;
		public float sensorWidth;   // in mm
		public float sensorHeight;  // in mm
		public float focalLength;   // in mm
		public float lensCenterX;   // in mm
		public float lensCenterY;   // in mm
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public byte[] distortionModel;
        public fixed float distortion[30];
        public fixed float extrinsic[12];
        public fixed float intrinsic[9];
    };

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CapturyLatencyInfo
	{
		public Int64 firstImagePacketTime;
		public Int64 optimizationStartTime;
		public Int64 optimizationEndTime;
		public Int64 packetSentTime;
		public Int64 packetReceivedTime;
		public Int64 timestampOfCorrespondingPose;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CapturyAngleData
	{
		public UInt16 type;
		public float value;
	}

	//====================
	// the network plugin
	//====================
	public class CapturyNetworkPlugin
	{
		public static CapturyNetworkPlugin Instance;
		enum ActorStatus { ACTOR_SCALING = 0, ACTOR_TRACKING = 1, ACTOR_STOPPED = 2, ACTOR_DELETED = 3, ACTOR_UNKNOWN = 4 };
		public enum PhysiologicalAngleType : ushort
		{
			LeftKneeFlexionExtension = 1,
			LeftKneeVarusValgus = 2,
			LeftKneeRotation = 3, // both internal and external
			LeftHipFlexionExtension = 4,
			LeftHipAbadduction = 5, // both ab- and adduction
			LeftHipRotation = 6, // both internal and external
			LeftAnkleFlexionExtension = 7,
			LeftAnklePronationSupination = 8,
			LeftAnkleRotation = 9,
			LeftShoulderFlexionExtension = 10,
			LeftShoulderTotalFlexion = 11,
			LeftShoulderAbadduction = 12, // both ab- and adduction
			LeftShoulderRotation = 13,
			LeftElbowFlexionExtension = 14,
			LeftForearmPronationSupination = 15,
			LeftWristFlexionExtension = 16,
			LeftWristRadialUlnarDeviation = 17,
			RightKneeFlexionExtension = 18,
			RightKneeVarusValgus = 19,
			RightKneeRotation = 20, // both internal and external
			RightHipFlexionExtension = 21,
			RightHipAbadduction = 22, // both ab- and adduction
			RightHipRotation = 23, // both internal and external
			RightAnkleFlexionExtension = 24,
			RightAnklePronationSupination = 25,
			RightAnkleRotation = 26,
			RightShoulderFlexionExtension = 27,
			RightShoulderTotalFlexion = 28,
			RightShoulderAbadduction = 29, // both ab- and adduction
			RightShoulderRotation = 30,
			RightElbowFlexionExtension = 31,
			RightForearmPronationSupination = 32,
			RightWristFlexionExtension = 33,
			RightWristRadialUlnarDeviation = 34,
			NeckFlexionExtension = 35,
			NeckRotation = 36,
			NeckLateralBending = 37,
			CenterOfGravityX = 38,
			CenterOfGravityY = 39,
			CenterOfGravityZ = 40,
			HeadRotation = 41,
			TorsoRotation = 42,
			TorsoInclination = 43,
			HeadInclination = 44,
			TorsoFlexion = 45
		};

		public enum ConnectionStatus : int
		{
			Disconnected = 0,
			Connecting = 1,
			Connected = 2
		}

		//=============================================
		// import the functions from RemoteCaptury dll
		//=============================================
		[DllImport("RemoteCaptury")]
		private static extern IntPtr Captury_create();
		[DllImport("RemoteCaptury")]
		private static extern int Captury_destroy(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_connect2(IntPtr rc, string ip, ushort port, ushort localPort, ushort localStreamPort, int async, UInt32 localAddress, UInt32 multicastAddress);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_disconnect(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_getConnectionStatus(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_getActors(IntPtr rc, out IntPtr actorData);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_startStreaming(IntPtr rc, int what);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_startStreamingImagesAndAngles(IntPtr rc, int what, int cameraId, int numAngles, IntPtr angles);
		[DllImport("RemoteCaptury")]
		private static extern IntPtr Captury_getCurrentPose(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_freePose(IntPtr pose);
		[DllImport("RemoteCaptury")]
		private static extern IntPtr Captury_getCurrentAngles(IntPtr rc, int actorId, out int numAngles);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_setRotationConstraint(IntPtr rc, int actorId, int jointIndex, IntPtr rotation, Int64 timestamp, float weight);
		[DllImport("RemoteCaptury")]
		private static extern Int64 Captury_synchronizeTime(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern Int64 Captury_getTime(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern Int64 Captury_getTimeOffset(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern IntPtr Captury_getLastErrorMessage(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_freeErrorMessage(IntPtr msg);
		// returns the number of cameras
		// on exit *cameras points to an array of CapturyCamera
		// if waitTime == 0, locally cached cameras are returned.
		// if waitTime < 0 it sends off the request for cameras and returns immediately.
		// if waitTime > 0, a request is sent, the specified amount waited and the cameras returned.
		// the returned array is owned by the library - do not free
		[DllImport("RemoteCaptury")]
		private static extern int Captury_getCameras(IntPtr rc, out IntPtr cameras, int waitTimeMs);
		[DllImport("RemoteCaptury")]
		private static extern IntPtr Captury_getCurrentARTags(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_freeARTags(IntPtr arTags);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_snapActorEx(IntPtr rc, float x, float z, float radius, float heading, [MarshalAs(UnmanagedType.LPStr)] string skeletonName, int snapMethod, int quickScaling);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_getTrackingQuality(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_getScalingProgress(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_getActorStatus(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		private static extern int Captury_captureBackground(IntPtr rc, IntPtr callback, IntPtr userData);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_rescaleActor(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_recolorActor(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_updateActorColors(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_stopTracking(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_deleteActor(IntPtr rc, int actorId);
		[DllImport("RemoteCaptury")]
		//[return: MarshalAs(UnmanagedType.LPStr)]
		private static extern IntPtr Captury_getStatus(IntPtr rc);
		[DllImport("RemoteCaptury")]
		private static extern void Captury_getCurrentLatency(IntPtr rc, IntPtr latencyInfo);
		[DllImport("RemoteCaptury")]
		private static extern IntPtr Captury_getNextLogMessage(IntPtr rc);

		public enum SnapMode { SNAP_BACKGROUND_LOCAL, SNAP_BACKGROUND_GLOBAL, SNAP_BODYPARTS_LOCAL, SNAP_BODYPARTS_GLOBAL, SNAP_BODYPARTS_JOINTS, SNAP_DEFAULT };

		[Flags]
		public enum CapturyStream
		{
			Nothing = 0x0000,
			Poses = 0x0001,
			GlobalPoses = 0x0001,  // Same as Poses
			LocalPoses = 0x0003,
			ArTags = 0x0004,
			Images = 0x0008,
			MetaData = 0x0010,     // Only valid when streaming poses
			ImuData = 0x0020,
			LatencyInfo = 0x0040,
			FootContact = 0x0080,
			Compressed = 0x0100,
			Angles = 0x0200,
			Scales = 0x0400,
			Blendshapes = 0x0800,
			Tcp = 0x1000,
			OnlyRootTranslation = 0x2000
		}

		public string host = "127.0.0.1";
		public ushort port = 2101;
		public string localIP = "0.0.0.0";
		public string multicastIP = "239.255.210.1";
		public float scaleFactor = 0.001f; // mm to m
		public int actorCheckTimeout = 500; // in ms
		public bool streamARTags = false;
		public bool usePoseCompression = false;
		public PhysiologicalAngleType[] angles = new PhysiologicalAngleType[0];

		private IntPtr remoteCaptury = IntPtr.Zero;

		// Events
		public delegate void SkeletonDelegate(CapturySkeleton skeleton);
		public event SkeletonDelegate SkeletonFound;
		public event SkeletonDelegate SkeletonLost;
		public event SkeletonDelegate ScalingProgressChanged;
		//public delegate void CamerasChangedDelegate(GameObject[] cameras);
		//public event CamerasChangedDelegate CamerasChanged;
		public delegate void ARTagsDetectedDelegate(ARTag[] artags);
		public event ARTagsDetectedDelegate ARTagsDetected;

		public delegate void PoseUpdateDelegate(Int32 id, Int64 timestamp);
		public event PoseUpdateDelegate PoseUpdateReceived;

		public event SkeletonDelegate AngleUpdateReceived;

		public Vector3[] cameraPositions;
        public Quaternion[] cameraOrientations;
        public float[] cameraFieldOfViews;
		//public GameObject[] cameras;

		public ARTag[] arTags = new ARTag[0];

		//public TextMeshProUGUI log;
		//public TextMeshProUGUI statusText;
		private String asyncLog = "";

		//public GameObject streamedSkeleton; // this is used to set CapturySkeleton.setReferenceSkeleton()
		//public Avatar streamedAvatar;
		public String streamedSkeletonLeftHip = "LeftUpLeg";
		public String streamedSkeletonHead = "Head";

		/// <summary>
		/// set the offset in world coordinates by moving the object the
		/// </summary>
		private Vector3 worldPosition = Vector3.Zero;
		private Quaternion worldRotation = Quaternion.Identity;

		// threading data for communication with server
		private Thread communicationThread;
		private readonly Mutex communicationMutex = new Mutex();
		private bool communicationFinished = false;

		// skeleton data from Captury
		private readonly Dictionary<int, int> actorFound = new Dictionary<int, int>();
		public readonly Dictionary<int, CapturySkeleton> skeletons = new Dictionary<int, CapturySkeleton>();
		private readonly Dictionary<string, int> jointsWithConstraints = new Dictionary<string, int>();

		// for debugging latency
		public bool measureLatency = false;
		public struct Timestamps
		{
			public Int64 pose;
			public Int64 update;
			public CapturyLatencyInfo latencyInfo;
			public Timestamps(Int64 p, Int64 up, CapturyLatencyInfo li)
			{
				pose = p;
				update = up;
				latencyInfo = li;
			}
		};

		public Dictionary<int, Timestamps> timestampsForPoses = new Dictionary<int, Timestamps>();
		private IntPtr latencyBuffer = IntPtr.Zero;

		//[Tooltip("Freeze the current positions, for debug purposes")]
		[System.NonSerialized]
		public bool freezeAvatars = false;

		private static System.Threading.Thread mainThread;

		// Mutex safeguards
		public int mutexTimeout = 300; // ms

		public string GetHost()
		{
			return host;
		}

		//public void Disconnect()
		//{
		//	Debug.Log("Captury: disconnecting...");
		//	Captury_disconnect(remoteCaptury);
		//	Debug.Log("Captury: disconnected");
		//}

		public ConnectionStatus GetConnectionStatus()
		{
			int status = Captury_getConnectionStatus(remoteCaptury);
			return (ConnectionStatus)status;
		}

		//public void SetHost(string ip)
		//{
		//	Debug.Log($"CapturyNetworkPlugin: setting host IP to {ip}");
		//	// parse the string ip to be valid

		//	// ip string must be of length 4
		//	string[] ipParts = ip.Split('.');
		//	if (ipParts.Length != 4) {
		//		Debug.Log("Invalid IP address");
		//		return;
		//	}

		//	// each split must be a number
		//	foreach (string part in ipParts) {
		//		int num;
		//		if (!int.TryParse(part, out num)) {
		//			Debug.Log("Invalid IP address");
		//			return;
		//		}
		//	}
		//	// set the ip address
		//	host = ip;

		//	Debug.Log($"Captury: connecting to {host}:{port}...");
		//	Captury_connect2(remoteCaptury, host, port, 0, 21010, 1/*async*/, convertToIP4(localIP), convertToIP4(multicastIP));

		//	Debug.Log("Captury: host set to " + host);
		//}

		//void Awake()
		//{
		//	Log("Captury: Awake");
		//	mainThread = System.Threading.Thread.CurrentThread;
		//	// try to set retargeter if available
		//	CapturySkeleton.networkPlugin = this;
		//	if (CapturyNetworkPlugin.Instance == null) {
		//		CapturyNetworkPlugin.Instance = this;
		//	}

		//	if (streamedAvatar == null || !streamedAvatar.isHuman || !streamedAvatar.isValid)
		//		LogError("CapturyNetworkPlugin.streamedAvatar must be set and humanoid.");
		//}

		//=============================
		// this is run once at startup
		//=============================
		public void Enable()
		{
			//worldPosition = transform.position;
			//worldRotation = transform.rotation;

			remoteCaptury = Captury_create();
			Log($"Captury: OnEnable, RemoteCaptury instance: {remoteCaptury}");
			Captury_connect2(remoteCaptury, host, port, 0, 21010, 1/*async*/, convertToIP4(localIP), convertToIP4(multicastIP));

			// set what you want to stream once and for all
			CapturyStream streamWhat = CapturyStream.Poses | CapturyStream.OnlyRootTranslation;
			if (streamARTags)
				streamWhat |= CapturyStream.ArTags;
			if (measureLatency)
				streamWhat |= CapturyStream.LatencyInfo;
			if (usePoseCompression)
				streamWhat |= CapturyStream.Compressed;
			if (angles.Length == 0)
			{
				if (Captury_startStreaming(remoteCaptury, (int)streamWhat) == 1)
				{
					Log("Captury: Successfully started streaming data");
				}
				else
					LogWarning("Captury: failed to start streaming");
			}
			else
			{
				IntPtr ptr = Marshal.AllocHGlobal(angles.Length * 2);
				for (int i = 0; i < angles.Length; ++i)
					Marshal.WriteInt16(ptr, i * 2, (short)angles[i]);
				try
				{
					streamWhat |= CapturyStream.Angles;
					if (Captury_startStreamingImagesAndAngles(remoteCaptury, (int)streamWhat, -1, angles.Length, ptr) == 1)
					{
						Log("Captury: Successfully started streaming data");
					}
					else
						LogWarning("Captury: failed to start streaming");
				}
				finally
				{
					Marshal.FreeHGlobal(ptr);
				}
			}

			// start the connection thread
			communicationFinished = false;
			communicationThread = new Thread(LookForActors);
			communicationThread.Start();
		}

		//==========================
		// this is run once at exit
		//==========================
		public void Disable()
		{
			Log($"Captury: disabling network plugin {remoteCaptury}");
			communicationFinished = true;
			communicationThread.Join();
			//ClearCameras();

			Captury_destroy(remoteCaptury);
			remoteCaptury = IntPtr.Zero;
		}

		//void ClearCameras()
		//{
		//	if (cameras != null) {
		//		Debug.LogWarning($"CapturyNetworkPlugin: ClearCameras {cameras.Length} entries");
		//		for (int i = cameras.Length - 1; i >= 0; i--) {
		//			Destroy(cameras[i]);
		//		}
		//		cameras = new GameObject[0];
		//	}
		//}

		//============================
		// this is run once per frame
		//============================
		public void Update()
		{
			//switch (GetConnectionStatus())
			//{
			//	case ConnectionStatus.Disconnected:
			//		if (statusText)
			//			statusText.text = "Disconnected";
			//		return;
			//	case ConnectionStatus.Connecting:
			//		if (statusText)
			//			statusText.text = "Connecting";
			//		return;
			//	case ConnectionStatus.Connected:
			//		if (statusText)
			//			statusText.text = "Connected";
			//		break; // only update when connected
			//}

			LogRemoteCapturyLogs();

			//worldPosition = transform.position;
			//worldRotation = transform.rotation;

			Int64 before = Captury_getTime(remoteCaptury);

			// make sure we lock access before doing anything
			//            Log("Captury: Starting pose update...");
			// Added timeout to prevent indefinite freeze
			if (!communicationMutex.WaitOne(mutexTimeout))
			{
				HandleMutexError($"CapturyNetworkPlugin.Update(): Failed to acquire communication mutex within timeout of {mutexTimeout}ms");
				return;
			}
			try
			{
				// fetch current pose for all skeletons
				foreach (KeyValuePair<int, CapturySkeleton> kvp in skeletons)
				{
					// get the actor id
					int actorId = kvp.Key;

					// skeleton does not have a reference yet. set it.
					if (skeletons[actorId].joints.Length == 1)
					{
						skeletons[actorId].joints[0].originalRotation = Quaternion.Identity;
					}
//					else if (!skeletons[actorId].Reference)
//					{
//						float lHipY = -1.0f;
//						float headY = -1.0f;
//						float backLen = -1.0f;
//						foreach (CapturySkeletonJoint j in kvp.Value.joints)
//						{
//							if (j.name == streamedSkeletonLeftHip)
//							{
//								lHipY = j.offset.Y;
//								for (int i = j.parent; i != -1; i = kvp.Value.joints[i].parent)
//									lHipY += kvp.Value.joints[i].offset.Y;
//							}
//							else if (j.name == streamedSkeletonHead)
//							{
//								headY = j.offset.Y;
//								for (int i = j.parent; i != -1; i = kvp.Value.joints[i].parent)
//									headY += kvp.Value.joints[i].offset.Y;
//							}
//						}
//						backLen = (lHipY != -1.0f && headY != -1.0f) ? (headY - lHipY) * 0.001f : -1.0f;
//						Log("Captury: streamed back length " + backLen);
//						// GameObject refSkel = new GameObject();
//						// Avatar av = CreateAvatar(skeletons[actorId], ref refSkel);
//						// skeletons[actorId].setReferenceSkeleton(refSkel, av, backLen);
//						GameObject refSkel = Instantiate(streamedSkeleton, null);
//						skeletons[actorId].SetReferenceSkeleton(refSkel, streamedAvatar, backLen);
//						refSkel.transform.SetParent(transform);
//						DumpSkeletons();
//#if DEBUG_VISUALS
//						refSkel.SetActive(true);
//#endif
//					}

					// get pointer to pose
					IntPtr poseData = Captury_getCurrentPose(remoteCaptury, actorId);

					// check if we actually got data, if not, continue
					if (poseData == IntPtr.Zero)
					{
						// something went wrong, get error message
						IntPtr msg = Captury_getLastErrorMessage(remoteCaptury);
						string errmsg = Marshal.PtrToStringAnsi(msg);
						Log("Captury: Stream error " + actorId + ": " + errmsg);
						//Captury_freeErrorMessage(msg);
					}
					else
					{
						// convert the pose
						CapturyPose pose;
						pose = (CapturyPose)Marshal.PtrToStructure(poseData, typeof(CapturyPose));

						// store timestamp stats for measuring latency
						if (measureLatency)
						{
							Int64 now = Captury_getTime(remoteCaptury);
							if (latencyBuffer == IntPtr.Zero)
								latencyBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CapturyLatencyInfo)));
							Captury_getCurrentLatency(remoteCaptury, latencyBuffer);
							CapturyLatencyInfo latencyInfo = new CapturyLatencyInfo();
							latencyInfo = (CapturyLatencyInfo)Marshal.PtrToStructure(latencyBuffer, typeof(CapturyLatencyInfo));
							if (latencyInfo.timestampOfCorrespondingPose == pose.timestamp)
							{
								timestampsForPoses[pose.actor] = new Timestamps(pose.timestamp, now, latencyInfo);
								// Log("Captury: got latency: img: " + (latencyInfo.optimizationStartTime - latencyInfo.firstImagePacketTime) + " opt: " + (latencyInfo.optimizationEndTime - latencyInfo.optimizationStartTime) + " net: " + (latencyInfo.packetReceivedTime - latencyInfo.packetSentTime) + " ->unity: " + (now - latencyInfo.packetReceivedTime) + " total: " + (now - latencyInfo.firstImagePacketTime));
								Captury_synchronizeTime(remoteCaptury);
							}
						}

						// copy the data into a float array
						float[] transforms = new float[pose.numTransforms * 6];
						Marshal.Copy(pose.transforms, transforms, 0, pose.numTransforms * 6);
						float[] blendShapeActivations = new float[pose.numBlendShapes];
						Marshal.Copy(pose.blendShapeActivations, blendShapeActivations, 0, pose.numBlendShapes);

						Captury_freePose(poseData);

						// Log("Captury: id " + pose.actor + " " + actorId + ": " + transforms[0] + " " + transforms[2] + " " + skeletons.Count + " " + actorId);

						Vector3 pos = new Vector3();
						Vector3 rot = new Vector3();

						if (!freezeAvatars)
						{
							// directly update pose of reference skeleton
							for (int jointID = 0; jointID < Math.Min(skeletons[actorId].joints.Length, pose.numTransforms); jointID++)
							{
								// ignore any joints that do not map to a transform
								if (skeletons[actorId].joints[jointID].transform == null)
								{
									//Log("Captury: skipping joint " + jointID + " " + skeletons[actorId].joints[jointID].name);
									continue;
								}

								Quaternion orig = skeletons[actorId].joints[jointID].originalRotation;

								// Log("Captury: at joint " + jointID + " " + skeletons[actorId].joints[jointID].name + " " + ConvertRotation(rot) + " " + ConvertPosition(pos));

								// set offset and rotation
								int baseIndex = jointID * 6;
								//if (jointID == 0)
								{ // only set translation for root joint
									pos = new Vector3(transforms[baseIndex + 0], transforms[baseIndex + 1], transforms[baseIndex + 2]);
									skeletons[actorId].joints[jointID].transform.TranslationVector = ConvertPosition(pos);
								}
                                rot = new Vector3(transforms[baseIndex + 3], transforms[baseIndex + 4], transforms[baseIndex + 5]);
								skeletons[actorId].joints[jointID].orientation = rot;// * orig);
								if (pose.numTransforms > 1)
									skeletons[actorId].joints[jointID].transform.ScaleVector = skeletons[actorId].joints[jointID].scale;
								skeletons[actorId].blendShapeActivations = blendShapeActivations;
							}

							skeletons[actorId].isLeftFootOnGround = ((pose.flags & 0x01) != 0);
							skeletons[actorId].isRightFootOnGround = ((pose.flags & 0x02) != 0);

							//skeletons[actorId].UpdatePose();
						}

						// trigger event:
						if (PoseUpdateReceived != null)
							PoseUpdateReceived(pose.actor, pose.timestamp);

						// get angles
						if (angles.Length != 0)
						{
							int numAngles;
							IntPtr ptr = Captury_getCurrentAngles(remoteCaptury, actorId, out numAngles);
							if (numAngles != 0)
							{
								CapturyAngleData[] anglez = new CapturyAngleData[numAngles];
								int size = Marshal.SizeOf(typeof(CapturyAngleData));

								for (int i = 0; i < numAngles; i++)
								{
									IntPtr dataPtr = new IntPtr(ptr.ToInt64() + size * i);
									anglez[i] = (CapturyAngleData)Marshal.PtrToStructure(dataPtr, typeof(CapturyAngleData));
								}

								skeletons[actorId].UpdateAngles(anglez);

								// trigger event
								if (AngleUpdateReceived != null)
									AngleUpdateReceived(skeletons[actorId]);
							}
						}

						skeletons[actorId].trackingQuality = Captury_getTrackingQuality(remoteCaptury, actorId);

						int scaling = Captury_getScalingProgress(remoteCaptury, actorId);
						skeletons[actorId].scalingProgress = scaling;

						if (scaling != skeletons[actorId].scalingProgress && ScalingProgressChanged != null)
							ScalingProgressChanged(skeletons[actorId]);
					}
				}

				//if (cameras != null && cameraPositions != null && cameras.Length != cameraPositions.Length)
				//{
				//	Debug.LogWarning($"CapturyNetworkPlugin: changing nrCameras from {cameras.Length} to {cameraPositions.Length}");
				//	cameras = new GameObject[cameraPositions.Length];
				//	for (int i = 0; i < cameraPositions.Length; ++i)
				//	{
				//		cameras[i] = new GameObject("Camera " + (i + 1));
				//		cameras[i].transform.SetParent(transform);
				//		cameras[i].AddComponent(typeof(Camera));
				//		cameras[i].SetActive(false);
				//		cameras[i].transform.SetPositionAndRotation(cameraPositions[i], cameraOrientations[i]);
				//		Camera cam = cameras[i].GetComponent(typeof(Camera)) as Camera;
				//		cam.fieldOfView = cameraFieldOfViews[i];
				//	}
				//	// Fire cameras changed event
				//	if (CamerasChanged != null)
				//		CamerasChanged(cameras);
				//}

				// get artags
				IntPtr arTagData = Captury_getCurrentARTags(remoteCaptury);

				// check if we actually got data, if not, continue
				if (arTagData == IntPtr.Zero)
				{
					// something went wrong, get error message
					//IntPtr msg = Captury_getLastErrorMessage();
					//string errmsg = Marshal.PtrToStringAnsi(msg);
					//Captury_freeErrorMessage(msg);
				}
				else
				{
					IntPtr at = arTagData;
					int num;
					for (num = 0; num < 100; ++num)
					{
						CapturyARTag arTag = (CapturyARTag)Marshal.PtrToStructure(at, typeof(CapturyARTag));
						if (arTag.id == -1)
							break;
						Array.Resize(ref arTags, num + 1);
						arTags[num] = new ARTag();
						arTags[num].id = arTag.id;
						arTags[num].translation = ConvertPosition(new Vector3(arTag.ox, arTag.oy, arTag.oz));
						arTags[num].rotation = ConvertRotation(new Vector3(arTag.nx, arTag.ny, arTag.nz)); // * Quaternion.Euler(new Vector3(0, 0, 90));
						at = new IntPtr(at.ToInt64() + Marshal.SizeOf(typeof(CapturyARTag)));
					}
					if (num != 0 && ARTagsDetected != null)
						ARTagsDetected(arTags);

					Captury_freeARTags(arTagData);
				}
			}
			catch (Exception e)
			{
				//Debug.LogError($"CapturyNetworkPlugin: Exception in Update(), while holding mutex! "
				//			+ $"This could have lead to a 'freeze'! Msg: {e}");
			}
			finally
			{
				communicationMutex.ReleaseMutex(); // Always release!
			}

			CapturySkeleton.Cleanup();

			lock (asyncLog)
			{
				//if (log)
				//	log.text += asyncLog;
				asyncLog = "";
			}
		}

		//================================================
		// This function continously looks for new actors
		// It runs in a separate thread
		//================================================
		unsafe void LookForActors()
		{
			try
			{

				while (!communicationFinished)
				{
					// wait for actorCheckTimeout ms before continuing
					Thread.Sleep(actorCheckTimeout);
					if (communicationFinished)
					{ // this might have changed during the sleep!
						LogWarning("Captury: communicationFinished was set while we were sleeping...");
						break;
					}

					// try to connect to captury live
					if (cameraPositions is null)
					{
						IntPtr cameraData = IntPtr.Zero;
						int numCameras = Captury_getCameras(remoteCaptury, out cameraData, 100);
						if (numCameras > 0 && cameraData != IntPtr.Zero)
						{
                            cameraPositions = new Vector3[numCameras];
							cameraOrientations = new Quaternion[numCameras];
							cameraFieldOfViews = new float[numCameras];
							int szStruct = Marshal.SizeOf(typeof(CapturyCamera));
							for (uint i = 0; i < numCameras; i++)
							{
								CapturyCamera camera = new CapturyCamera();
								camera = (CapturyCamera)Marshal.PtrToStructure(new IntPtr(cameraData.ToInt64() + (szStruct * i)), typeof(CapturyCamera));
								cameraPositions[i] = ConvertPosition(new Vector3(camera.positionX, camera.positionY, camera.positionZ));
								cameraOrientations[i] = ConvertRotation(new Vector3(camera.orientationX, camera.orientationY, camera.orientationZ));// * new Quaternion(0, 0.7071f, 0, 0.7071f);
								cameraFieldOfViews[i] = (float)(Math.Atan2(camera.focalLength, 0.5f * camera.sensorWidth) * (camera.sensorHeight / camera.sensorWidth) * 2 * 180 / Math.PI);
							}
						}
					}

					// get actors
					IntPtr actorData = IntPtr.Zero;
					int numActors = Captury_getActors(remoteCaptury, out actorData);
					if (numActors > 0 && actorData != IntPtr.Zero)
					{
						if (numActors != skeletons.Count)
							Log($"Captury: Received {numActors} actors, we had {skeletons.Count} skeletons already");

						//	 create actor struct
						int szStruct = Marshal.SizeOf(typeof(CapturyActor)); // implicit padding
						for (uint i = 0; i < numActors; i++)
						{
							//get an actor
						    CapturyActor actor = new CapturyActor();
							actor = (CapturyActor)Marshal.PtrToStructure(new IntPtr(actorData.ToInt64() + (szStruct * i)), typeof(CapturyActor));

							if (Captury_getActorStatus(remoteCaptury, actor.id) > (int)ActorStatus.ACTOR_TRACKING)
								continue;

							//		 check if we already have it in our dictionary
							if (skeletons.ContainsKey(actor.id))
							{ // access to actors does not need to be locked here because the other thread is read-only
								actorFound[actor.id] = 2;
								continue;
							}
							Log("Captury: Found new actor " + actor.id);

							//		 no? we need to convert it
							CapturySkeleton skeleton = new CapturySkeleton();
							if (!ConvertActor(actor, actorData, ref skeleton))
								continue;

							if (SkeletonFound != null)
								SkeletonFound(skeleton);

							//		  and add it to the list of actors we are processing, making sure this is secured by the mutex
							if (!communicationMutex.WaitOne(mutexTimeout))
							{
								HandleMutexError($"CapturyNetworkPlugin.lookForActors(), adding skeleton {i}/{numActors}: Failed to acquire mutex within {mutexTimeout}ms");
								continue; // Skip this operation, try again later
							}
							try
							{
								skeletons.Add(actor.id, skeleton);
								actorFound.Add(actor.id, 2);
							}
							catch (Exception e)
							{
								HandleMutexError($"CapturyNetworkPlugin: Exception in Update(), adding skeleton {i}/{numActors}, while holding mutex! "
																				+ $"This could have lead to a 'freeze'! Msg: {e}");
							}
							finally
							{
								communicationMutex.ReleaseMutex(); // always release!
							}
							DumpSkeletons();
						}
					}

					// reduce the actor countdown by one for each actor
					int[] keys = new int[actorFound.Keys.Count];
					actorFound.Keys.CopyTo(keys, 0);
					foreach (int key in keys)
						actorFound[key]--;

					// remove all actors that were not found in the past few actor checks
					// First, collect the skeletons to remove while holding the mutex briefly
					List<CapturySkeleton> skeletonsToNotify = new List<CapturySkeleton>();
					List<int> unusedKeys = new List<int>();

					if (!communicationMutex.WaitOne(mutexTimeout))
					{
						HandleMutexError($"CapturyNetworkPlugin.lookForActors(), removing old actors: Failed to acquire mutex within {mutexTimeout}ms");
						continue; // Skip this operation, try again later
					}

					try
					{
						foreach (KeyValuePair<int, int> kvp in actorFound)
						{
							if (Captury_getActorStatus(remoteCaptury, kvp.Key) <= (int)ActorStatus.ACTOR_TRACKING)
								continue;

							if (SkeletonLost != null)
							{
								// dumpSkeletons();
								Log("Captury: lost skeleton " + kvp.Key + ". telling all my friends.");
								skeletonsToNotify.Add(skeletons[kvp.Key]);
							}

							// remove actor
							skeletons.Remove(kvp.Key);
							unusedKeys.Add(kvp.Key);
						}
					}
					catch (Exception e)
					{
						HandleMutexError($"CapturyNetworkPlugin.lookForActors(), removing old actors: exception happened while holding mutex: {e}");
					}
					finally
					{
						communicationMutex.ReleaseMutex();
					}

					// Deactivate and notify outside the mutex to prevent deadlocks
					foreach (var skeleton in skeletonsToNotify)
					{
						if (SkeletonLost != null)
							SkeletonLost(skeleton);
						skeleton.Deactivate();
					}

					// clear out actorfound structure
					foreach (int key in unusedKeys)
						actorFound.Remove(key);
				}
			}
			catch (DllNotFoundException e)
			{
				LogError("Captury: RemoteCaptury.dll/libRemoteCaptury.so could not be loaded");
				LogError($"Exception: {e}: {e.Message}");
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
#endif
			}
			catch (EntryPointNotFoundException ex)
			{
				LogError("Captury: RemoteCaptury.dll/libRemoteCaptury.so does not provide symbol: " + ex.Message);
#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
#endif
			}
		}

		void HandleMutexError(string msg)
		{
			//Debug.LogError(msg);
		}

		//	// if heading > 360 heading is considered unknown
		//	public void SnapActor(float x, float z, float radius, float heading = 720, string name = "", SnapMode snapMethod = SnapMode.SNAP_DEFAULT, bool quickScaling = false)
		//	{
		//		Captury_snapActorEx(remoteCaptury, x, z, radius, heading, name, (int)snapMethod, quickScaling ? 1 : 0);
		//	}

		//	public void RescaleActor(CapturySkeleton skel)
		//	{
		//		Captury_rescaleActor(remoteCaptury, skel.id);
		//	}

		//	public void RecolorActor(CapturySkeleton skel)
		//	{
		//		Captury_recolorActor(remoteCaptury, skel.id);
		//	}

		//	public void UpdateActorColors(CapturySkeleton skel)
		//	{
		//		Captury_updateActorColors(remoteCaptury, skel.id);
		//	}

		//	public void StopTracking(CapturySkeleton skel)
		//	{
		//		Captury_stopTracking(remoteCaptury, skel.id);
		//	}

		//	public void DeleteActor(CapturySkeleton skel)
		//	{
		//		Log("Captury: deleting skeleton " + skel.id);
		//		Captury_deleteActor(remoteCaptury, skel.id);
		//	}

		//	public string GetCapturyLiveStatus()
		//	{
		//		return Marshal.PtrToStringAnsi(Captury_getStatus(remoteCaptury));
		//	}

		//	public int getNumCameras()
		//	{
		//		IntPtr cameraData = IntPtr.Zero;
		//		int numCameras = Captury_getCameras(remoteCaptury, out cameraData, 100);
		//		return numCameras;
		//	}

		public void LogRemoteCapturyLogs()
		{
			while (true)
			{
				IntPtr msg = Captury_getNextLogMessage(remoteCaptury);
				if (msg == IntPtr.Zero)
					break;

				//Debug.Log("RemoteCaptury: " + Marshal.PtrToStringAnsi(msg));
			}
		}

		//	public void SetRotationConstraint(int id, string jointName, Transform t)
		//	{
		//		if (!communicationMutex.WaitOne(mutexTimeout)) {
		//			HandleMutexError($"setRotationConstraint: Failed to acquire communication mutex within {mutexTimeout}ms");
		//			return;
		//		}

		//		if (!skeletons.ContainsKey(id)) {
		//			Log("Cannot set rotation for " + jointName + ": no skeleton with id " + id);
		//			communicationMutex.ReleaseMutex();
		//			return;
		//		}

		//		Log("Captury: Set " + jointName + "-rotation to " + t);
		//		CapturySkeleton skel = skeletons[id];
		//		communicationMutex.ReleaseMutex();

		//		int index;
		//		if (jointsWithConstraints.ContainsKey(jointName))
		//			index = jointsWithConstraints[jointName];
		//		else {
		//			index = 0;
		//			foreach (CapturySkeletonJoint j in skel.joints) {
		//				if (j.name == jointName)
		//					break;
		//				++index;
		//			}
		//			if (index == skel.joints.Length) {
		//				Log("Cannot set constraint for joint " + jointName + ": no such joint");
		//				return;
		//			}
		//		}

		//		//        CapturySkeletonJoint jnt = skel.joints[index];
		//		Vector3 euler = ConvertToEulerAngles(ConvertRotationToLive(t.rotation));
		//		IntPtr rotation = Marshal.AllocHGlobal(12);
		//		Marshal.StructureToPtr(euler, rotation, false);
		//		Captury_setRotationConstraint(remoteCaptury, id, index, rotation, Captury_getTime(remoteCaptury), 1.0f);
		//		Marshal.FreeHGlobal(rotation);
		//	}

		//===============================================
		// helper function to map an actor to a skeleton
		//===============================================
		private bool ConvertActor(CapturyActor actor, IntPtr actorData, ref CapturySkeleton skel)
		{
			if (skel == null)
			{
				Log("Captury: Null skeleton reference");
				return false;
			}

			// copy data over
			skel.name = System.Text.Encoding.UTF8.GetString(actor.name);
			skel.name = skel.name.Remove(skel.name.IndexOf('\0'));
			skel.id = actor.id;
			//skel.rawData = actorData;

			int[] parents = new int[actor.numJoints];
			if (actor.numJoints == 29)
			{
				parents[0] = -1; // hips
				parents[1] = 0; // spine
				parents[2] = 1; // spine1
				parents[3] = 2; // spine2
				parents[4] = 3; // spine3
				parents[5] = 4; // spine4
				parents[6] = 5; // neck
				parents[7] = 6; // head
				parents[8] = 7; // headee
				parents[9] = 4; // leftshoulder
				parents[10] = 9; // leftarm
				parents[11] = 10; // leftforearm
				parents[12] = 11; // lefthand
				parents[13] = 12; // lefthandee
				parents[14] = 4; // rightshoulder
				parents[15] = 14; // rightarm
				parents[16] = 15; // rightforearm
				parents[17] = 16; // righthand
				parents[18] = 17; // righthandee
				parents[19] = 0; // leftupleg
				parents[20] = 19; // leftleg
				parents[21] = 20; // leftfoot
				parents[22] = 21; // lefttoebase
				parents[23] = 22; // leftfootee
				parents[24] = 0; // rightupleg
				parents[25] = 24; // rightleg
				parents[26] = 25; // rightfoot
				parents[27] = 26; // righttoebase
				parents[28] = 27; // rightfootee
			}
			else if (actor.numJoints == 69)
			{
				parents[0] = -1; // hips
				parents[1] = 0; // spine
				parents[2] = 1; // spine1
				parents[3] = 2; // spine2
				parents[4] = 3; // spine3
				parents[5] = 4; // spine4
				parents[6] = 5; // neck
				parents[7] = 6; // head
				parents[8] = 7; // headee
				parents[9] = 4; // leftshoulder
				parents[10] = 9; // leftarm
				parents[11] = 10; // leftforearm

				parents[12] = 11; // lefthand
				parents[13] = 12; // leftthumb1
				parents[14] = 13; // leftthumb2
				parents[15] = 14; // leftthumb3
				parents[16] = 15; // leftthumbee
				parents[17] = 12; // leftindex1
				parents[18] = 17; // leftindex2
				parents[19] = 18; // leftindex3
				parents[20] = 19; // leftindexee
				parents[21] = 12; // leftmiddle1
				parents[22] = 21; // leftmiddle2
				parents[23] = 22; // leftmiddle3
				parents[24] = 23; // leftmiddleee
				parents[25] = 12; // leftring1
				parents[26] = 25; // leftring2
				parents[27] = 26; // leftring3
				parents[28] = 27; // leftringee
				parents[29] = 12; // leftpinky1
				parents[30] = 29; // leftpinky2
				parents[31] = 30; // leftpinky3
				parents[32] = 31; // leftpinkyee
				parents[33] = 12; // lefthandee

				parents[34] = 4; // rightshoulder
				parents[35] = 34; // rightarm
				parents[36] = 35; // rightforearm

				parents[37] = 36; // righthand
				parents[38] = 37; // leftthumb1
				parents[39] = 38; // leftthumb2
				parents[40] = 39; // leftthumb3
				parents[41] = 40; // leftthumbee
				parents[42] = 37; // leftindex1
				parents[43] = 42; // leftindex2
				parents[44] = 43; // leftindex3
				parents[45] = 44; // leftindexee
				parents[46] = 37; // leftmiddle1
				parents[47] = 46; // leftmiddle2
				parents[48] = 47; // leftmiddle3
				parents[49] = 48; // leftmiddleee
				parents[50] = 37; // leftring1
				parents[51] = 50; // leftring2
				parents[52] = 51; // leftring3
				parents[53] = 52; // leftringee
				parents[54] = 37; // leftpinky1
				parents[55] = 54; // leftpinky2
				parents[56] = 55; // leftpinky3
				parents[57] = 56; // leftpinkyee
				parents[58] = 37; // righthandee

				parents[59] = 0; // leftupleg
				parents[60] = 19; // leftleg
				parents[61] = 20; // leftfoot
				parents[62] = 21; // lefttoebase
				parents[63] = 22; // leftfootee
				parents[64] = 0; // rightupleg
				parents[65] = 24; // rightleg
				parents[66] = 25; // rightfoot
				parents[67] = 26; // righttoebase
				parents[68] = 27; // rightfootee
			}
			else if (actor.numJoints == 1)
			{
				parents[0] = -1;
			}
			else
			{
				LogWarning("Captury: cannot convert full actor. wrong number of joints. expected 1, 29 or 69, have " + actor.numJoints + ". trying my best...");
				int szStructx = Marshal.SizeOf(typeof(CapturyJoint));
				for (uint i = 0; i < actor.numJoints; i++)
				{
					// marshall the joints into a new joint struct
					CapturyJoint joint = new CapturyJoint();
					joint = (CapturyJoint)Marshal.PtrToStructure(new IntPtr(actor.joints.ToInt64() + (szStructx * i)), typeof(CapturyJoint));

					string name = System.Text.Encoding.ASCII.GetString(joint.name);
					int jpos = name.IndexOf("\0");
					name = name.Substring(0, jpos);
					LogWarning("Captury: joint " + i + ": " + name);
					parents[i] = joint.parent;
				}
			}
			// create joints
			int szStruct = Marshal.SizeOf(typeof(CapturyJoint));
			skel.joints = new CapturySkeletonJoint[actor.numJoints];
			for (uint i = 0; i < actor.numJoints; i++)
			{
				// marshall the joints into a new joint struct
				CapturyJoint joint = new CapturyJoint();
				joint = (CapturyJoint)Marshal.PtrToStructure(new IntPtr(actor.joints.ToInt64() + (szStruct * i)), typeof(CapturyJoint));

				skel.joints[i] = new CapturySkeletonJoint();
				skel.joints[i].name = System.Text.Encoding.ASCII.GetString(joint.name.TakeWhile(x => x != 0).ToArray());
				//int jpos = skel.joints[i].name.IndexOf('\0');
				//skel.joints[i].name = skel.joints[i].name.Substring(0, jpos);
				skel.joints[i].offset = new Vector3(joint.ox, joint.oy, joint.oz);
				skel.joints[i].orientation = new Vector3(joint.rx, joint.ry, joint.rz);
				skel.joints[i].scale = new Vector3(joint.sx, joint.sy, joint.sz);
				skel.joints[i].parent = parents[i];

				// Log("Captury: Got joint " + skel.joints[i].name + " at " + joint.ox + " " + joint.oy + " " + joint.oz);
			}
			// blend shapes
			if (actor.numBlendShapes != 0)
			{
				skel.blendShapeNames = new string[actor.numBlendShapes];
				byte[] byteArray = new byte[64];
				for (uint i = 0; i < actor.numBlendShapes; i++)
				{
					Marshal.Copy((IntPtr)(actor.blendShapes.ToInt64() + i * 64), byteArray, 0, 64);
					skel.blendShapeNames[i] = System.Text.Encoding.ASCII.GetString(byteArray).TrimEnd('\0'); ;
				}
				skel.blendShapeActivations = new float[actor.numBlendShapes];
			}

			return true;
		}

		// helper function for debugging
		// print the currently known skeletons
		public void DumpSkeletons()
		{
			// debug deleting
			foreach (KeyValuePair<int, CapturySkeleton> kvp in skeletons)
			{
				int actorId = kvp.Key;
				CapturySkeleton skel = kvp.Value;
				//Log("Captury: skeleton " + actorId + " " + skel.name + " " + (skel.Reference ? (" ref: " + skel.referenceName) : "no reference") + " " + (skel.Target ? (" tgt: " + skel.targetName) : " no target"), skel.Reference);
			}
		}

		//	//===============================================
		//	// helper function to map an actor to a skeleton
		//	//===============================================
		//	private Avatar CreateAvatar(CapturySkeleton skel, ref GameObject root)
		//	{
		//		if (skel == null) {
		//			LogWarning("Captury: Null skeleton reference");
		//			return null;
		//		}

		//		int numJoints = skel.joints.Length;

		//		HumanDescription desc = new HumanDescription();
		//		desc.skeleton = new SkeletonBone[numJoints + 1];
		//		desc.human = new HumanBone[numJoints + 1];
		//		string[] mecanimNames = HumanTrait.BoneName;
		//		// GameObject master = GameObject.CreatePrimitive(PrimitiveType.Cube);
		//		GameObject[] hierarchy = new GameObject[numJoints];

		//		string[] mecNames = new string[numJoints];
		//		mecNames[0] = "Hips"; // hips
		//		mecNames[1] = "Spine"; // spine
		//		mecNames[2] = "Chest"; // spine1
		//		mecNames[3] = ""; // spine2
		//		mecNames[4] = "UpperChest"; // spine3
		//		mecNames[5] = ""; // spine4
		//		mecNames[6] = "Neck"; // neck
		//		mecNames[7] = "Head"; // head
		//		mecNames[8] = ""; // headee
		//		mecNames[9] = "LeftShoulder"; // leftshoulder
		//		mecNames[10] = "LeftUpperArm"; // leftarm
		//		mecNames[11] = "LeftLowerArm"; // leftforearm
		//		mecNames[12] = "LeftHand"; // lefthand
		//		mecNames[13] = ""; // lefthandee
		//		mecNames[14] = "RightShoulder"; // rightshoulder
		//		mecNames[15] = "RightUpperArm"; // rightarm
		//		mecNames[16] = "RightLowerArm"; // rightforearm
		//		mecNames[17] = "RightHand"; // righthand
		//		mecNames[18] = ""; // righthandee
		//		mecNames[19] = "LeftUpperLeg"; // leftupleg
		//		mecNames[20] = "LeftLowerLeg"; // leftleg
		//		mecNames[21] = "LeftFoot"; // leftfoot
		//		mecNames[22] = "LeftToes"; // lefttoebase
		//		mecNames[23] = ""; // leftfootee
		//		mecNames[24] = "RightUpperLeg"; // rightupleg
		//		mecNames[25] = "RightLowerLeg"; // rightleg
		//		mecNames[26] = "RightFoot"; // rightfoot
		//		mecNames[27] = "RightToes"; // righttoebase
		//		mecNames[28] = ""; // rightfootee

		//		root.name = "Root";

		//		// create joints
		//		for (uint i = 0, n = 1; i < numJoints; i++) {
		//			//
		//			desc.skeleton[i + 1].name = skel.joints[i].name;
		//			desc.skeleton[i + 1].position = skel.joints[i].offset * 0.001f;
		//			desc.skeleton[i + 1].rotation = Quaternion.identity;
		//			desc.skeleton[i + 1].scale = skel.joints[i].scale;

		//			if (mecNames[i].Length != 0) {
		//				desc.human[n].boneName = skel.joints[i].name;
		//				desc.human[n].humanName = mecNames[i];
		//				desc.human[n].limit.useDefaultValues = true;
		//				++n;
		//			}

		//			hierarchy[i] = new GameObject();
		//			// hierarchy[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
		//			// hierarchy[i].transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
		//			if (i > 0 && skel.joints[i].parent >= 0)
		//				hierarchy[i].transform.SetParent(hierarchy[skel.joints[i].parent].transform);
		//			else
		//				hierarchy[i].transform.SetParent(root.transform);
		//			hierarchy[i].transform.position = hierarchy[i].transform.parent.transform.position + skel.joints[i].offset * 0.001f;
		//			hierarchy[i].name = skel.joints[i].name;

		//			//Log("Captury: Got joint " + skel.joints[i].name + " at " + joint.ox + joint.oy + joint.oz);
		//		}

		//		desc.human[0].boneName = "Root";
		//		desc.human[0].humanName = "Root";
		//		desc.human[0].limit.useDefaultValues = true;

		//		desc.skeleton[0].name = "Root";
		//		desc.skeleton[0].position = Vector3.zero;
		//		desc.skeleton[0].rotation = Quaternion.identity;
		//		desc.skeleton[0].scale = Vector3.one;
		//		// hierarchy[0].transform.SetParent(hierarchy[0].transform);

		//		Avatar av = AvatarBuilder.BuildHumanAvatar(root, desc);
		//		if (av.isValid) {
		//			Log("created valid avatar from thin air");
		//			return av;
		//		} else {
		//			Log("Captury: failed to create avatar");
		//			return null;
		//		}
		//	}

		//	public Int64 GetTime()
		//	{
		//		return (Int64)Captury_getTime(remoteCaptury);
		//	}

		public static uint convertToIP4(string ipAddress)
		{
			byte[] bytes = IPAddress.Parse(ipAddress).GetAddressBytes();
			// Convert to uint32 in network order (big-endian)
			return ((uint)bytes[0] << 24) |
				   ((uint)bytes[1] << 16) |
				   ((uint)bytes[2] << 8) |
				   ((uint)bytes[3]);
		}

		//========================================================================================================
		// Helper function to convert a position from a right-handed to left-handed coordinate system (both Y-up)
		//========================================================================================================
		public Vector3 ConvertPosition(Vector3 position)
		{
			return position; 

			position.X *= scaleFactor;
			position.Y *= scaleFactor;
			position.Z *= scaleFactor;
			return worldRotation * new Vector3(position.Z, position.Y, position.X) + worldPosition;
		}

		////========================================================================================================
		//// Helper function to convert a position from a left-handed to right-handed coordinate system (both Y-up)
		////========================================================================================================
		//public Vector3 ConvertPositionToLive(Vector3 position)
		//{
		//	position.X /= scaleFactor;
		//	position.Y /= scaleFactor;
		//	position.Z /= scaleFactor;
		//	return Quaternion.Inverse(worldRotation) * (new Vector3(position.Z, position.Y, position.X) - worldPosition);
		//}

		//===========================================================================================================================
		// Helper function to convert a rotation from a right-handed Captury Live to left-handed Unity coordinate system (both Y-up)
		//===========================================================================================================================
		public Quaternion ConvertRotation(Vector3 rotation)
		{
			Quaternion qx = Quaternion.RotationAxis(Vector3.UnitX, (float)(VLMath.DegToRad * rotation.X));
			Quaternion qy = Quaternion.RotationAxis(Vector3.UnitY, (float)(VLMath.DegToRad * rotation.Y));
			Quaternion qz = Quaternion.RotationAxis(Vector3.UnitZ, (float)(VLMath.DegToRad * rotation.Z));
			Quaternion qq = qx * qy * qz;

			return qq; 
			return worldRotation * qq;
		}

		////===========================================================================================================
		//// Helper function to convert a rotation from Unity back to Captury Live (left-handed to right-handed, Y-up)
		////===========================================================================================================
		//public static Quaternion ConvertRotationToLive(Quaternion rotation)
		//{
		//	Vector3 angles = rotation.eulerAngles;

		//	Quaternion qx = Quaternion.AngleAxis(angles.X, Vector3.UnitZ);
		//	Quaternion qy = Quaternion.AngleAxis(angles.Y, Vector3.UnitY);
		//	Quaternion qz = Quaternion.AngleAxis(angles.Z, Vector3.UnitX);
		//	Quaternion qq = qz * qy * qx;
		//	return qq;
		//}

		//	//=============================================================================
		//	// Helper function to convert a rotation to the Euler angles Captury Live uses
		//	//=============================================================================
		//	public static Vector3 ConvertToEulerAngles(Quaternion quat)
		//	{
		//		const float RAD2DEGf = 57.29577951308232088f;
		//		Vector3 euler = new Vector3();
		//		float sqw = quat.w * quat.w;
		//		float sqx = quat.x * quat.x;
		//		float sqy = quat.y * quat.y;
		//		float sqz = quat.z * quat.z;
		//		float tmp1 = quat.x * quat.y;
		//		float tmp2 = quat.w * quat.z;
		//		euler[1] = (float)-Math.Asin(Math.Min(Math.Max(2.0 * (quat.x * quat.z - quat.y * quat.w), -1.0f), 1.0f));
		//		float C = (float)Math.Cos(euler[1]);
		//		if (Math.Abs(C) > 0.005) {
		//			euler[2] = (float)Math.Atan2(2.0 * (quat.x * quat.y + quat.z * quat.w) / C, (sqx - sqy - sqz + sqw) / C) * RAD2DEGf;
		//			euler[0] = (float)Math.Atan2(2.0 * (quat.y * quat.z + quat.x * quat.w) / C, (-sqx - sqy + sqz + sqw) / C) * RAD2DEGf;
		//		} else {
		//			euler[2] = 0;
		//			if ((tmp1 - tmp2) < 0)
		//				euler[0] = (float)Math.Atan2((quat.x * quat.y - quat.z * quat.w) - (quat.y * quat.z - quat.x * quat.w), ((-sqx + sqy - sqz + sqw) + 2.0 * (quat.x * quat.z + quat.y * quat.w)) * 0.5f) * RAD2DEGf;
		//			else
		//				euler[0] = (float)Math.Atan2((quat.x * quat.y - quat.z * quat.w) + (quat.y * quat.z - quat.x * quat.w), ((-sqx + sqy - sqz + sqw) - 2.0 * (quat.x * quat.z + quat.y * quat.w)) * 0.5f) * RAD2DEGf;
		//		}
		//		euler[1] *= RAD2DEGf;

		//		if (Double.IsNaN(euler[0]) || Double.IsNaN(euler[1]) || Double.IsNaN(euler[2]))
		//			return euler;

		//		return euler;
		//	}

		//	public bool IsConnected
		//	{
		//		get { return GetConnectionStatus() == ConnectionStatus.Connected; }
		//	}

		//	public int numSkeletons {
		//		get { return skeletons.Count; }
		//	}

		public void Log(String msg)
		{
			//Debug.Log(msg);
			//if (log && mainThread.Equals(System.Threading.Thread.CurrentThread))
			//	log.text += "\n" + msg;
			//else
			//{
			//	lock (asyncLog)
			//		asyncLog += "\n" + msg;
			//}
		}

		//public void Log(String msg, UnityEngine.Object context)
		//{
		//	Debug.Log(msg, context);
		//	if (log && mainThread.Equals(System.Threading.Thread.CurrentThread))
		//		log.text += "\n" + msg;
		//	else
		//	{
		//		lock (asyncLog)
		//			asyncLog += "\n" + msg;
		//	}
		//}

		public void LogWarning(String msg)
		{
			//Debug.LogWarning(msg);
			//if (log && mainThread.Equals(System.Threading.Thread.CurrentThread))
			//	log.text += "\nWarning: " + msg;
			//else
			//{
			//	lock (asyncLog)
			//		asyncLog += "\nWarning: " + msg;
			//}
		}

		//public void LogWarning(String msg, UnityEngine.Object context)
		//{
		//	Debug.LogWarning(msg, context);
		//	if (log && mainThread.Equals(System.Threading.Thread.CurrentThread))
		//		log.text += "\nWarning: " + msg;
		//	else
		//	{
		//		lock (asyncLog)
		//			asyncLog += "\nWarning: " + msg;
		//	}
		//}

		public void LogError(String msg)
		{
			//Debug.LogError(msg);
			//if (log && mainThread.Equals(System.Threading.Thread.CurrentThread))
			//	log.text = "\nError: " + msg;
			//else
			//{
			//	lock (asyncLog)
			//		asyncLog += "\nError: " + msg;
			//}
		}

		//public void LogError(String msg, UnityEngine.Object context)
		//{
		//	Debug.LogError(msg, context);
		//	if (log && mainThread.Equals(System.Threading.Thread.CurrentThread))
		//		log.text = "\nError: " + msg;
		//	else
		//	{
		//		lock (asyncLog)
		//			asyncLog += "\nError: " + msg;
		//	}
		//}
		//}

		////==========================================================================
		//// Helper extension function to get all children from a specified transform
		////==========================================================================
		//public static class TransformExtension
		//{
		//	public static ArrayList GetAllChildren(this Transform transform)
		//	{
		//		ArrayList children = new ArrayList();
		//		foreach (Transform child in transform) {
		//			children.Add(child);
		//			children.AddRange(GetAllChildren(child));
		//		}
		//		return children;
		//	}
		//}
	}
}
