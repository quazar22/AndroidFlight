using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System.Net;
using System;
using UnityEngine.UI;
using System.IO;

public class TCPServer : MonoBehaviour
{

    private static List<ServerClient> clients;
    internal static List<ServerClient> lobbyList;
    public Text port;
    internal static TcpListener server;
    static private bool serverStarted;
    private TCPClient hostingclient;

    float t = 0;
    
    //////////////////////////-------- BEGIN FUNCTIONS --------//////////////////////////
    void Start()
    {
        if(serverStarted) { return; }
        clients = new List<ServerClient>();
        lobbyList = new List<ServerClient>();

        try
        {
            server = new TcpListener(IPAddress.Any, Int32.Parse(port.text));
            server.Start();

            StartListening();
            serverStarted = true;
        }
        catch (Exception e)
        {
            Debug.Log("Socket Error: " + e.Message);
        }
        hostingclient.SetupSocket("127.0.0.1", port.text);
        hostingclient.enabled = true;
    }

    public void SetClient(GameObject gameobj)
    {
        hostingclient = gameobj.GetComponent<TCPClient>();
    }

    private void StartListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }

    void Update()
    {
        if (!serverStarted)
        {
            return;
        }
    }

    private void FixedUpdate()
    {
        //Debug.Log("TIME: "+ (t/Time.time));
        //if (shouldSpawn)
        //{
        //    GameObject ClientPlane = Instantiate(planeObj, new Vector3(0f, spawnPosition, -24238.8f), Quaternion.identity);
        //    shouldSpawn = false;
        //    spawnPosition -= 20f;
        //}
        foreach (ServerClient client in clients)
        {
            if (!IsConnected(client.tcp))
            {
                RemoveClientFromGame(client);
                String output = ServerToClientUpdater(client, Commands.GET_LOBBY, null);
                SendToAll(output);
                continue;
            }
            else
            {
                NetworkStream s = client.tcp.GetStream();
                if (s.DataAvailable)
                {
                    string received_data = client.ReadMessage();

                    String[] data = CommandParse(received_data);
                    int cmd;
                    bool result = Int32.TryParse(data[0], out cmd); //op code
                    if(!result)
                    {
                        continue;
                    }
                    string args = data[1];          //arguments

                    if (data != null)
                    {
                        OnIncomingData(client, received_data); //display incoming text in debug
                        String output = ServerToClientUpdater(client, cmd, args);
                        SendToAll(output); //$data should be $output
                    }
                }
            }
        }
        t++;
    }

    private void RemoveClientFromGame(ServerClient c)
    {
        c.CloseSocket();
        lobbyList.Remove(c);
        clients.Remove(c);
        for (int i = 1; i < clients.Count; i++)
        {
            clients[i].userId = i;
            clients[i].SendMessage(CommandCreator(Commands.SET_CLIENT_INDEX) + i.ToString());
        }
    }

    private void RemoveClientFromLobby(ServerClient c)
    {
        lobbyList.RemoveAt(lobbyList.IndexOf(c));
    }

    public static string CommandCreator(int cmd)
    {
        return cmd.ToString() + Delimiters.CMD_DELIM.ToString();
    }


    //might want to implement error handling at some point
    //probably a returned integer which would detail the type of error
    internal static void SendToAll(String ToSend)
    {
        if(ToSend.Length == 0) { return; }
        
        foreach (ServerClient c in clients)
        {
            c.SendMessage(ToSend);
        }
    }

    private String ServerToClientUpdater(ServerClient client, int cmd, string args)
    {
        String output = "";
        switch (cmd)
        {
            case Commands.ADD_TO_LOBBY:
                Commands.AddToLobby(client, args);
                goto case Commands.UPDATE_LOBBY;
            case Commands.UPDATE_LOBBY:
                output += Commands.UpdateLobby(client, args); //client sends 21#name;ready
                break;
            case Commands.GET_LOBBY:
                output += Commands.UpdateLobby();
                break;
            case Commands.REMOVED_FROM_LOBBY:
                RemoveClientFromLobby(client);
                output += Commands.UpdateLobby();
                break;
            case Commands.BEGIN_GAME:
                output += Commands.BeginGame();
                break;
            case Commands.PLANE_POSITION:
                output += Commands.SendPlanePosition(args);
                break;
        }
        return output;
    }

    internal static String[] CommandParse(String input)
    {
        if(input.Length == 0) { return null; }
        return input.Split(Delimiters.CMD_DELIM);
    }

    internal static String[] ArgParse(String input)
    {
        String[] argvec = null;
        if (input == null || input.Length == 0) { return argvec; }
        return input.Split(Delimiters.ATTRIB_DELIM);
    }

    private bool IsConnected(TcpClient c)
    {
        try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private void AcceptTcpClient(IAsyncResult AR)
    {
        Debug.Log("Connected!");
        TcpListener listener = (TcpListener)AR.AsyncState;
        clients.Add(new ServerClient(listener.EndAcceptTcpClient(AR), clients.Count));
        Commands.UpdateUserIndex(clients[clients.Count - 1], clients[clients.Count - 1].userId);
        StartListening();
    }

    private void OnApplicationQuit()
    {

    }

    private void OnIncomingData(ServerClient c, string data)
    {
        Debug.Log("Server Received: " + data); //logs to debug console
    }

}

public class ServerClient
{
    public TcpClient tcp;
    public string clientName;
    public int userId;
    public int ready;
    public StreamWriter sw;
    public StreamReader sr;
    
    public ServerClient(TcpClient clientSocket, int id)
    {
        clientName = "Guest";
        ready = 0;
        tcp = clientSocket;
        userId = id;
        NetworkStream ns = tcp.GetStream();
        sr = new StreamReader(ns, true);
        sw = new StreamWriter(ns);
    }

    public void SendMessage(string msg)
    {
        sw.WriteLine(msg);
        sw.Flush();
    }

    public string ReadMessage()
    {
        return sr.ReadLine();
    }

    public void CloseSocket()
    {
        tcp.Close();
    }
}

static public class Commands
{
    public const int PASS = 0;
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

    private static String[] CommandParse(String input)
    {
        if (input == null || input.Length == 0) { return null; }
        return input.Split(Delimiters.CMD_DELIM);
    }

    private static String[] ArgParse(String input)
    {
        if (input == null || input.Length == 0) { return null; }
        return input.Split(Delimiters.ATTRIB_DELIM);
    }

    private static String[] PlayerParse(String input)
    {
        if (input == null || input.Length == 0) { return null; }
        return input.Split(Delimiters.PLAYER_DELIM);
    }


    static public void SendClientCustom(ServerClient client, int command, string args)
    {
        client.SendMessage(command.ToString() + Delimiters.CMD_DELIM + args);
    }

    static public void UpdateUserIndex(ServerClient client, int index)
    {
        client.SendMessage(SET_CLIENT_INDEX.ToString() + Delimiters.CMD_DELIM + index.ToString());
    }

    //20#name;ready
    /// FUTURE (HOPEFULLY) => 20#name#planetype
    static public void AddToLobby(ServerClient client, string args)
    {
        String[] argvec = ArgParse(args);
        client.clientName = argvec[0];
        client.ready = Int32.Parse(argvec[1]);
        TCPServer.lobbyList.Add(client);
    }

    //     0    1
    //21#name;ready
    /// FUTURE (HOPEFULLY) => 21#name;planetype;ready@name;planetype;ready...
    static public string UpdateLobby(ServerClient client, string args)
    {
        String[] argvec = TCPServer.ArgParse(args);
        client.clientName = argvec[0];
        client.ready = Int32.Parse(argvec[1]);
        return UpdateLobby();
    }

    static public string UpdateLobby()
    {
        String outString = TCPServer.CommandCreator(UPDATE_LOBBY);
        foreach (ServerClient c in TCPServer.lobbyList)
        {
            outString += c.clientName;
            outString += Delimiters.ATTRIB_DELIM;
            outString += c.ready;
            if (!((TCPServer.lobbyList.Count - 1) == TCPServer.lobbyList.IndexOf(c)))
            {
                outString += Delimiters.PLAYER_DELIM;
            }
        }
        return outString;
    }

    public static string SendPlanePosition(string args)
    {
        return TCPServer.CommandCreator(PLANE_POSITION) + args;
    }

    static public string BeginGame()
    {
        float[] x = new float[] {0};
        float y = 275f;
        float[] z = new float[] {0, 100, 200, 300, 400, 500};
        foreach(ServerClient client in TCPServer.lobbyList)
        {
            if (client.ready == 0)
                return "";
        }
        string planes = "";

        foreach(ServerClient client in TCPServer.lobbyList)
        {
            Quaternion q = Quaternion.identity;
            planes += client.userId.ToString() +
                    Delimiters.ATTRIB_DELIM + x[0] +
                    Delimiters.ATTRIB_DELIM + y +
                    Delimiters.ATTRIB_DELIM + z[client.userId] +
                    Delimiters.ATTRIB_DELIM + q.x +
                    Delimiters.ATTRIB_DELIM + q.y +
                    Delimiters.ATTRIB_DELIM + q.z +
                    Delimiters.ATTRIB_DELIM + q.w;
            if (!((TCPServer.lobbyList.Count - 1) == TCPServer.lobbyList.IndexOf(client)))
            {
                planes += Delimiters.PLAYER_DELIM;
            }
        }
        //TCPServer.server.Stop();
        return TCPServer.CommandCreator(BEGIN_GAME) + planes;
    }
}

public class Delimiters
{
    public const char CMD_DELIM = '#';
    public const char ATTRIB_DELIM = ';';
    public const char PLAYER_DELIM = '@';
}
