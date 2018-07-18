using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TCPClient : MonoBehaviour
{

    TcpClient tcpClient = null;
    public Text host;
    public Text port;
    internal static Player clientplayer;
    internal static List<Player> playerList;
    internal static NetworkStream ns;
    internal static StreamWriter socket_writer = null;
    internal static StreamReader socket_reader = null;
    internal static Text NameInput;
    internal static Button BeginButton;
    internal static Button ReadyButton;
    internal static GameObject planeObj;
    internal static GameObject GenericPlane;
    internal static GameObject Networking;
    internal static List<Text> LobbyArray;
    internal static CameraMovement mainCamera;
    internal static Slider slider;
    internal static Button MenuCtrl;
    internal static PlaneMovement p;

    void Start()
    {
        Networking = GameObject.Find("Networking");
        planeObj = (GameObject)Resources.Load("Prefabs/PlaneObj");
        GenericPlane = (GameObject)Resources.Load("Prefabs/GenericPlane");
        playerList = new List<Player>();
        clientplayer = new Player();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string currentscene = SceneManager.GetActiveScene().name;
        if (currentscene == "Selection")
        {
            NameInput = GameObject.Find("NameInput").GetComponentInChildren<Text>();
            Button b = GameObject.Find("NextButton").GetComponent<Button>();
            b.onClick.AddListener(Commands.LoadLobby);
        }
        if (currentscene == "Lobby")
        {
            BeginButton = GameObject.Find("BeginButton").GetComponent<Button>();
            ReadyButton = GameObject.Find("ReadyButton").GetComponent<Button>();
            if (clientplayer.id != 0)
            {
                DestroyImmediate(BeginButton);
            }
            else
            {
                BeginButton.onClick.AddListener(Commands.SendBegin);
            }
            ReadyButton.onClick.AddListener(Commands.SendReady);
            LobbyArray = new List<Text>();
            for (int i = 0; i < Commands.MAX_PLAYERS; i++)
            {
                LobbyArray.Add(GameObject.Find("Player" + i).GetComponent<Text>());
            }
            Commands.AddToLobby();
        }
        if (currentscene == "Game")
        {
            clientplayer.planeobj = Instantiate(planeObj, new Vector3(clientplayer.pos.x, clientplayer.pos.y, clientplayer.pos.z), Quaternion.identity);
            clientplayer.planeobj.name = "planeObj";
            mainCamera = GameObject.Find("Main Camera").GetComponent<CameraMovement>(); //works
            mainCamera.reference = clientplayer.planeobj.GetComponentsInChildren<Transform>()[1];
            mainCamera.posF = 0.5f;
            mainCamera.rotF = 0.1f;
            p = clientplayer.planeobj.GetComponent<PlaneMovement>();
            p.joystick = GameObject.Find("BackgroundImage").GetComponentInChildren<Joystick>();
            p.fire = GameObject.Find("Fire").GetComponent<FireButton>();
            slider = GameObject.Find("Slider").GetComponent<Slider>();
            slider.onValueChanged.AddListener(delegate { p.updateSpeed(slider); } );
            MenuCtrl = GameObject.Find("MenuCtrl").GetComponent<Button>();
            MenuCtrl.onClick.AddListener(clientplayer.planeobj.GetComponentInChildren<PlaneMovement>().SetJoystick);
            foreach(Player player in playerList)
            {
                player.planeobj = Instantiate(GenericPlane, player.pos, player.rot);
            }
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.GetKeyUp(KeyCode.Escape) && SceneManager.GetActiveScene().name == "Lobby")
            {
                Commands.RemovedFromLobby();
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
                return;
            }
        }
        if (SceneManager.GetActiveScene().name == "Game")
        {
            Vector3 p = clientplayer.planeobj.transform.position;
            Quaternion q = clientplayer.planeobj.transform.rotation;
            string outString = Commands.CommandCreator(Commands.PLANE_POSITION);
            outString += clientplayer.id.ToString() + Delimiters.ATTRIB_DELIM +
                         p.x + Delimiters.ATTRIB_DELIM +
                         p.y + Delimiters.ATTRIB_DELIM +
                         p.z + Delimiters.ATTRIB_DELIM +
                         q.x + Delimiters.ATTRIB_DELIM +
                         q.y + Delimiters.ATTRIB_DELIM +
                         q.z + Delimiters.ATTRIB_DELIM +
                         q.w;
            WriteSocket(outString);
        }
    }

    private void FixedUpdate()
    {
        String received_data = ReadSocket();
        if (received_data.Length > 0)
        {
            Debug.Log("CLIENT RECEIVED: " + received_data);

            String[] data = CommandParse(received_data);
            int cmd = Int32.Parse(data[0]); //op code
            string args = data[1];          //arguments

            switch (SceneManager.GetActiveScene().name)
            {
                case "MainScreen":
                    PerformMainScreenCommand(cmd, args);
                    DontDestroyOnLoad(Networking);
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    SceneManager.LoadScene("Selection", LoadSceneMode.Single);
                    break;
                case "Selection":
                    PerformSelectionCommand(cmd, args);
                    break;
                case "Lobby":
                    PerformLobbyCommand(cmd, args);
                    break;
                case "Game":
                    PerformGameCommand(cmd, args);
                    break;
            }
        }
    }

    private void PerformSelectionCommand(int cmd, string args)
    {
        switch (cmd)
        {
            case Commands.SET_CLIENT_INDEX:
                clientplayer.id = Int32.Parse(args);
                break;
        }
    }

    private String[] CommandParse(String input)
    {
        return input.Split(Delimiters.CMD_DELIM);
    }

    private void PerformMainScreenCommand(int cmdNum, String args)
    {
        switch (cmdNum)
        {
            case Commands.SET_CLIENT_INDEX:
                clientplayer.id = Int32.Parse(args);
                break;
        }
    }

    private void PerformLobbyCommand(int cmdNum, String args)
    {
        switch (cmdNum)
        {
            case Commands.SET_CLIENT_INDEX:
                clientplayer.id = Int32.Parse(args);
                break;
            case Commands.UPDATE_LOBBY:
                Commands.UpdateLobby(args);
                break;
            case Commands.STARTING_COORDINATES:
                break;
            case Commands.BEGIN_GAME:
                Commands.LoadGame(args);
                break;
        }
    }

    private void PerformGameCommand(int cmdNum, String args)
    {
        switch (cmdNum)
        {
            case Commands.PLANE_POSITION:
                UpdatePlanePosition(args);
                break;
        }
    }

    private void UpdatePlanePosition(String args)
    {
        string[] argvec = Commands.ArgParse(args);
        foreach(Player p in playerList)
        {
            if(p.id == Int32.Parse(argvec[0]))
            {
                SetPosition(p, argvec, Commands.ITERATIONS);
                SetRotation(p, argvec);
            }
        }
    }


    internal Vector3 ParsePosition(String args)
    {
        String[] argvec = Commands.ArgParse(args);
        Vector3 p = new Vector3
        {
            x = float.Parse(argvec[1]),
            y = float.Parse(argvec[2]),
            z = float.Parse(argvec[3])
        };
        return p;
    }

    internal Quaternion ParseRotation(String args)
    {
        String[] argvec = Commands.ArgParse(args);
        Quaternion q = new Quaternion
        {
            x = float.Parse(argvec[4]),
            y = float.Parse(argvec[5]),
            z = float.Parse(argvec[6]),
            w = float.Parse(argvec[7])
        };
        return q;
    }

    internal void SetPosition(Player client, string[] argvec, int iterations)
    {
        Vector3 newPos = new Vector3(float.Parse(argvec[1]),
                        float.Parse(argvec[2]),
                        float.Parse(argvec[3]));
        Vector3 oldPos = client.planeobj.transform.position;
        Vector3 iterVec =
            new Vector3((Math.Abs(newPos.x) - Math.Abs(oldPos.x)) / iterations,
                        (Math.Abs(newPos.y) - Math.Abs(oldPos.y)) / iterations,
                        (Math.Abs(newPos.z) - Math.Abs(oldPos.z)) / iterations);
        //for (int i = iterations; i > 0; i--)
        //{
        //    client.planeobj.transform.position += iterVec;
        //}
        client.planeobj.transform.position = newPos;
    }

    internal void SetRotation(Player client, string[] argvec)
    {
        Quaternion rot = new Quaternion(float.Parse(argvec[4]),
                           float.Parse(argvec[5]),
                           float.Parse(argvec[6]),
                           float.Parse(argvec[7])
                       );
        client.planeobj.transform.rotation = rot;
    }

    void OnApplicationQuit()
    {
        CloseSocket();
    }

    public void SetupSocket()
    {
        try
        {
            tcpClient = new TcpClient(host.text, Int32.Parse(port.text));
            ns = tcpClient.GetStream();
            socket_reader = new StreamReader(ns, true);
            socket_writer = new StreamWriter(ns);
            enabled = true;
        }
        catch (Exception e)
        {
            e.ToString();
            Debug.Log("Socket error");
        }
    }

    public void SetupSocket(string host, string port)
    {
        try
        {
            tcpClient = new TcpClient(host, Int32.Parse(port));
            ns = tcpClient.GetStream();
            socket_reader = new StreamReader(ns, true);
            socket_writer = new StreamWriter(ns);
        }
        catch (Exception e)
        {
            e.ToString();
            Debug.Log("Socket error");
        }
    }

    public String ReadSocket()
    {
        if (ns == null) { return ""; }

        if (!ns.DataAvailable)
        {
            return "";
        } else {
            return socket_reader.ReadLine();
        }
    }

    internal static void WriteSocket(string line)
    {
        if (ns == null) { return; }
        socket_writer.WriteLine(line);
        socket_writer.Flush();
    }

    public void CloseSocket()
    {
        if (socket_reader == null || socket_writer == null || tcpClient == null)
            return;

        socket_writer.Close();
        socket_reader.Close();
        tcpClient.Close();
    }

    public class Commands
    {
        public const int PASS = 0;
        public const int ITERATIONS = 10;
        public const int MAX_PLAYERS = 8;

        public const int SEND_DATA = 10;
        public const int INIT_CONNECTION = 11;
        public const int BEGIN_GAME = 12;

        public const int ADD_TO_LOBBY = 20;
        public const int UPDATE_LOBBY = 21;
        public const int GET_LOBBY = 22;
        public const int REMOVED_FROM_LOBBY = 23;
        public const int CLIENT_DISCONNECTED = 24;
        public const int START_ACCEPTING = 25;

        //in game commands
        public const int STARTING_COORDINATES = 30;
        public const int PLANE_POSITION = 31;

        public const int SET_CLIENT_INDEX = 40;

        public static String[] CommandParse(String input)
        {
            if (input == null || input.Length == 0) { return null; }
            return input.Split(Delimiters.CMD_DELIM);
        }

        public static String[] ArgParse(String input)
        {
            if(input == null || input.Length == 0) { return null; }
            return input.Split(Delimiters.ATTRIB_DELIM);
        }

        public static String[] PlayerParse(String input)
        {
            if (input == null || input.Length == 0) { return null; }
            return input.Split(Delimiters.PLAYER_DELIM);
        }

        
        public static void UpdateLobby(string args)
        {
            String[] PlayerStrings = PlayerParse(args);
            int i = 0;
            for (; i < PlayerStrings.Length; i++) {
                String[] playerargs = ArgParse(PlayerStrings[i]);
                LobbyArray[i].text = playerargs[0];
                LobbyArray[i].color = (playerargs[1] == "1") ? Color.green : Color.red;
            }
            for(; i < MAX_PLAYERS; i++)
            {
                LobbyArray[i].text = "Player " + (i + 1);
                LobbyArray[i].color = Color.black;
            }
        }

        public static void RemovedFromLobby()
        {
            WriteSocket(CommandCreator(REMOVED_FROM_LOBBY) + PASS);
        }

        public static string CommandCreator(int cmd)
        {
            return cmd.ToString() + Delimiters.CMD_DELIM.ToString();
        }

        public static void AddToLobby()
        {
            WriteSocket(CommandCreator(ADD_TO_LOBBY) + clientplayer.name + Delimiters.ATTRIB_DELIM.ToString() + clientplayer.ready);
        }

        public static void SendReady()
        {
            clientplayer.ready = (clientplayer.ready == "1") ? "0" : "1";
            ReadyButton.GetComponentInChildren<Text>().color = (clientplayer.ready == "1") ? Color.green : Color.red;
            WriteSocket(CommandCreator(UPDATE_LOBBY) + clientplayer.name + Delimiters.ATTRIB_DELIM.ToString() + clientplayer.ready);
        }

        public static void SendBegin()
        {
            WriteSocket(BEGIN_GAME.ToString() + Delimiters.CMD_DELIM.ToString() + "0");
        }

        public static void LoadLobby()
        {
            clientplayer.name = NameInput.text;
            DontDestroyOnLoad(Networking);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }

        public static void LoadGame(string args)
        {
            string[] players = PlayerParse(args);
            foreach (string playerargs in players)
            {
                string[] argvec = ArgParse(playerargs);
                if (argvec[0] != clientplayer.id.ToString())
                {
                    Player p = new Player
                    {
                        id = Int32.Parse(argvec[0]),
                        pos = new Vector3(float.Parse(argvec[1]),
                                          float.Parse(argvec[2]),
                                          float.Parse(argvec[3])
                                          ),
                        rot = new Quaternion(float.Parse(argvec[4]),
                                             float.Parse(argvec[5]),
                                             float.Parse(argvec[6]), 0),
                    };
                    playerList.Add(p);
                }
                else
                {
                    clientplayer.id = Int32.Parse(argvec[0]);
                    clientplayer.pos = new Vector3(float.Parse(argvec[1]),
                                          float.Parse(argvec[2]),
                                          float.Parse(argvec[3]));
                    clientplayer.rot = new Quaternion(float.Parse(argvec[4]),
                                             float.Parse(argvec[5]),
                                             float.Parse(argvec[6]), 0);
                }
            }
            DontDestroyOnLoad(Networking);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }

    public class Delimiters
    {
        public const char CMD_DELIM = '#';
        public const char ATTRIB_DELIM = ';';
        public const char PLAYER_DELIM = '@';
    }

    public class Player
    {
        public GameObject planeobj;
        public string name;
        public string ready;
        public int id;
        public Vector3 pos;
        public Quaternion rot;

        public Player()
        {
            ready = "0";
        }
    }
}
