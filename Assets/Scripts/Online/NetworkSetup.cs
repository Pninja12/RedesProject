using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Diagnostics;
using UnityEditor;
using System.Linq;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
#endif





#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif


public class NetworkSetup : MonoBehaviour
{
    [SerializeField] private GameTag localPlayerPrefab;
    [SerializeField] private GameTag remotePlayerPrefab;
    [SerializeField] private Grid playerMap;

    [SerializeField] private Transform[] spawnPoints;

    private GameObject spawnedMap;
    private bool isServer = false;
    private List<int> assignedPlayerNumbers = new List<int>();
    private Dictionary<ulong, int> clientPlayerNumbers = new Dictionary<ulong, int>();
    void Start()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(1280, 720, false); // Adjust size as needed
        // Parse command line arguments
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--server")
            {
                // --server found, this should be a server application
                isServer = true;
            }
        }
        if (isServer)
            StartCoroutine(StartAsServerCR());
        else
            StartCoroutine(StartAsClientCR());
    }

    private void SpawnGameTagForPlayer(ulong clientId, Vector3 position, int playerNum, int side)
    {
        var spawnedObject = Instantiate(localPlayerPrefab, position, Quaternion.identity);
        var netObj = spawnedObject.GetComponent<NetworkObject>();

        netObj.CheckObjectVisibility = (clientIdToCheck) => clientIdToCheck == clientId;
        netObj.SpawnWithOwnership(clientId);

        var tag = spawnedObject.GetComponent<GameTag>();
        tag.SetPlayerNumber(playerNum);
        tag.SetTile(playerNum);
        tag.SetSide(side);
    }


    IEnumerator StartAsServerCR()
    {
        SetWindowTitle("Starting up as server...");
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;

        // Wait a frame for setups to be done
        yield return null;

        if (networkManager.StartServer())
        {
            SetWindowTitle("Server");
            UnityEngine.Debug.Log($"Serving on port {transport.ConnectionData.Port}...");

            // Spawn global map
            if (playerMap != null && spawnedMap == null)
            {
                spawnedMap = Instantiate(playerMap.gameObject, Vector3.zero, Quaternion.identity);
                spawnedMap.GetComponent<NetworkObject>().Spawn();
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            SetWindowTitle("Failed to connect as server...");
            UnityEngine.Debug.LogError($"Failed to serve on port {transport.ConnectionData.Port}...");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (clientId == NetworkManager.Singleton.LocalClientId) return; // Don't spawn server-owned GameTags

        Vector3 spawnPos = new Vector3(0, 0, 0); // Player's own tag on the right
        int playerNum = clientPlayerNumbers.Count + 1;
        int side = playerNum == 1 ? 1 : -1;

        clientPlayerNumbers[clientId] = playerNum;

        // Spawn the networked tag (visible only to its owner)
        //SpawnGameTagForPlayer(clientId, spawnPos, playerNum, side);

        // Tell all *other* clients to create a mirrored version of this new tag
        foreach (var conn in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (conn == clientId) continue;

            SpawnMirroredTagClientRpc(
                clientId, 
                spawnPos,
                playerNum,
                -side,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { conn } }
                });
        }

        // Also, tell the new client to create mirrors of *existing* players
        foreach (var otherClientId in clientPlayerNumbers.Keys)
        {
            if (otherClientId == clientId) continue;

            int otherPlayerNum = clientPlayerNumbers[otherClientId];
            Vector3 otherPos = new Vector3(3, 0, 0); // Their original position
            int otherSide = otherPlayerNum == 1 ? 1 : -1;

            SpawnMirroredTagClientRpc(
                otherClientId,
                new Vector3(-otherPos.x, otherPos.y, otherPos.z),
                otherPlayerNum,
                -otherSide,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
        }
    }

    [ClientRpc]
    private void SpawnMirroredTagClientRpc(ulong originalClientId, Vector3 position, int playerNum, int side, ClientRpcParams rpcParams = default)
    {
        // Skip if this is the owning client OR if we're on the server
        if (NetworkManager.Singleton.IsServer || originalClientId == NetworkManager.Singleton.LocalClientId)
            return;

        var mirrored = Instantiate(remotePlayerPrefab, position, Quaternion.identity);
        mirrored.GetComponent<NetworkObject>().enabled = false; // Not networked

        var tag = mirrored.GetComponent<GameTag>();
        tag.SetPlayerNumber(playerNum);
        tag.SetTile(playerNum);
        tag.SetSide(side);
    }

    private int AssignPlayerNumber()
    {
        if (!assignedPlayerNumbers.Contains(1))
        {
            assignedPlayerNumbers.Add(1);
            return 1;
        }
        else if (!assignedPlayerNumbers.Contains(2))
        {
            assignedPlayerNumbers.Add(2);
            return 2;
        }
        else
        {
            // Add more slots or reject connection
            UnityEngine.Debug.LogWarning("Too many players! Returning fallback number 99.");
            return 99;
        }
    }

    private void OnLocalClientConnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        // Assign based on how many clients connected before
        int playerNum = NetworkManager.Singleton.ConnectedClients.Count == 1 ? 1 : 2;
        int side = playerNum == 1 ? 1 : -1;

        // Spawn your own GameTag on the right side
        var ownPos = new Vector3(3.5f * side, 0, 0);
        var ownTag = Instantiate(localPlayerPrefab, ownPos, Quaternion.identity);
        ownTag.SetPlayerNumber(playerNum);
        ownTag.SetTile(playerNum);
        ownTag.SetSide(side);

        // Spawn adversary's GameTag on the opposite side (they won't see this UI)
        var adversaryNum = playerNum == 1 ? 2 : 1;
        var adversarySide = -side;
        var adversaryPos = new Vector3(5 * adversarySide, 0, 0);
        var adversaryTag = Instantiate(remotePlayerPrefab, adversaryPos, Quaternion.identity);
        adversaryTag.SetPlayerNumber(adversaryNum);
        adversaryTag.SetTile(adversaryNum);
        adversaryTag.SetSide(adversarySide);
    }


    private void OnClientDisconnected(ulong clientId)
    {
        UnityEngine.Debug.Log($"Player {clientId} disconnected");
    }

    public class PlayerInfo : INetworkSerializable
    {
        public int playerNum;
        public int side;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            throw new NotImplementedException();
        }
    }

    IEnumerator StartAsClientCR()
    {
        SetWindowTitle("Starting up as client...");
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;

        yield return null;

        if (networkManager.StartClient())
        {
            SetWindowTitle("Client");
            UnityEngine.Debug.Log($"Connecting on port {transport.ConnectionData.Port}...");

            NetworkManager.Singleton.OnClientConnectedCallback += (_) =>
            {
                RequestPlayerInfoServerRpc();
            };
        }
        else
        {
            SetWindowTitle("Failed to connect as client...");
            UnityEngine.Debug.LogError($"Failed to connect on port {transport.ConnectionData.Port}...");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayerInfoServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        int playerNum;
        if (!clientPlayerNumbers.ContainsKey(clientId))
        {
            playerNum = AssignPlayerNumber();
            clientPlayerNumbers[clientId] = playerNum;
        }
        else
        {
            playerNum = clientPlayerNumbers[clientId];
        }

        int side = playerNum == 1 ? 1 : -1;

        SendPlayerInfoClientRpc(playerNum, side, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });
    }

    [ClientRpc]
    private void SendPlayerInfoClientRpc(int playerNum, int side, ClientRpcParams rpcParams = default)
    {
        Vector3 ownPos = new Vector3(3.5f * side, 0, 0);
        var ownTag = Instantiate(localPlayerPrefab, ownPos, Quaternion.identity);
        ownTag.SetPlayerNumber(playerNum);
        ownTag.SetTile(playerNum);
        ownTag.SetSide(side);

        int adversaryNum = playerNum == 1 ? 2 : 1;
        int adversarySide = -side;
        Vector3 adversaryPos = new Vector3(4 * adversarySide, 0, 0);
        var adversaryTag = Instantiate(remotePlayerPrefab, adversaryPos, Quaternion.identity);
        adversaryTag.SetPlayerNumber(adversaryNum);
        adversaryTag.SetTile(adversaryNum);
        adversaryTag.SetSide(adversarySide);
    }

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowText(IntPtr hWnd, string lpString);
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    static extern IntPtr EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    // Delegate to filter windows
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private static IntPtr FindWindowByProcessId(uint processId)
    {
        IntPtr windowHandle = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            uint windowProcessId;
            GetWindowThreadProcessId(hWnd, out windowProcessId);
            if (windowProcessId == processId)
            {
                windowHandle = hWnd;
                return false; // Found the window, stop enumerating
            }
            return true; // Continue enumerating
        }, IntPtr.Zero);
        return windowHandle;
    }
    static void SetWindowTitle(string title)
    {
#if !UNITY_EDITOR
        uint processId = (uint)Process.GetCurrentProcess().Id;
        IntPtr hWnd = FindWindowByProcessId(processId);
        if (hWnd != IntPtr.Zero)
        {
        SetWindowText(hWnd, title);
        }
#endif
    }
#else
        static void SetWindowTitle(string title)
        {
        }
#endif

#if UNITY_EDITOR
     [MenuItem("Tools/Build Windows (x64)", priority = 0)]
     public static bool BuildGame()
     {
        // Specify build options
         BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
         buildPlayerOptions.scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        buildPlayerOptions.locationPathName = Path.Combine("Builds", "ProjetoFinal.exe");
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;
        // Perform the build
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
           // Output the result of the build
        UnityEngine.Debug.Log($"Build ended with status: {report.summary.result}");
         // Additional log on the build, looking at report.summary
        return report.summary.result == BuildResult.Succeeded;
    }

    private static void Run(string path, string args)
    {
        // Start a new process
        Process process = new Process();
        // Configure the process using the StartInfo properties
        process.StartInfo.FileName = Path.GetFullPath(path);
        process.StartInfo.Arguments = args;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal; // Choose the window style: Hidden, Minimized, Maximized, Normal
        process.StartInfo.UseShellExecute = false; // Set to false if you want to redirect the output
        process.StartInfo.CreateNoWindow = false;
        process.StartInfo.RedirectStandardOutput = false; // Set to true to redirect the output (so you can read it in Unity)
         
        // Run the process
        process.Start();
    }

    [MenuItem("Tools/Build and Launch (Server)", priority = 10)]
    public static void BuildAndLaunch1()
    {
        CloseAll();
        if (BuildGame())
        {
            Launch1();
        }
    }
    [MenuItem("Tools/Build and Launch (Server + Client)", priority = 20)]
    public static void BuildAndLaunch2()
    {
        CloseAll();
        if (BuildGame())
        {
            Launch2();
        }
    }
    [MenuItem("Tools/Launch (Server) _F11", priority = 30)]
    public static void Launch1()
    {
        Run("Builds\\ProjetoFinal.exe", "--server");
    }
    [MenuItem("Tools/Launch (Server + Client)", priority = 40)]
    public static void Launch2()
    {
        Run("Builds\\ProjetoFinal.exe", "--server");
        Run("Builds\\ProjetoFinal.exe", "");
    }

    [MenuItem("Tools/Close All", priority = 100)]
    public static void CloseAll()
    {
        // Get all processes with the specified name
        Process[] processes = Process.GetProcessesByName("ProjetoFinal");
        foreach (var process in processes)
        {
            try
            {
                // Close the process
                process.Kill();
                // Wait for the process to exit
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                // Handle exceptions, if any
                // This could occur if the process has already exited or you don't have permission to kill it
                UnityEngine.Debug.LogWarning($"Error trying to kill process {process.ProcessName}: {ex.Message}");
            }
        }
    }
#endif



}
