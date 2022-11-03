using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatsonTcp;

namespace DoodleniteServerProvider {
internal class TestServer
{
    private static string _ServerIp = "";
    private static int _ServerPort = 0;
    private static bool _Ssl = false;
    private static WatsonTcpServer _Server = null;
    private static string _CertFile = "";
    private static string _CertPass = "";
    private static bool _DebugMessages = true;
    private static bool _AcceptInvalidCerts = true;
    private static bool _MutualAuth = true;
    private static string _LastIpPort;
    
    
    //Doodlenite
    private static string serverExePath = "D:/Work/Games/CGL_BA3_BattleRoyale_ServerBuilds/CGL_BA3_BattleRoyale.exe";
    
    private static List<string> clientsWaitingForNewServer = new List<string>();
    //

    private static void Main(string[] args)
    {
        _ServerIp = InputString("Server IP:", "localhost", false);
        _ServerPort = InputInteger("Server port:", 50880, true, false);
        _Ssl = InputBoolean("Use SSL:", false);

        try
        {
            if (!_Ssl)
            {
                _Server = new WatsonTcpServer(_ServerIp, _ServerPort);
            }
            else
            {
                _CertFile = InputString("Certificate file:", "test.pfx", false);
                _CertPass = InputString("Certificate password:", "password", false);
                _AcceptInvalidCerts = InputBoolean("Accept invalid certs:", true);
                _MutualAuth = InputBoolean("Mutually authenticate:", false);

                _Server = new WatsonTcpServer(_ServerIp, _ServerPort, _CertFile, _CertPass);
                _Server.Settings.AcceptInvalidCertificates = _AcceptInvalidCerts;
                _Server.Settings.MutuallyAuthenticate = _MutualAuth;
            }

            _Server.Events.ClientConnected += ClientConnected;
            _Server.Events.ClientDisconnected += ClientDisconnected;
            _Server.Events.MessageReceived += MessageReceived;
            _Server.Events.ServerStarted += ServerStarted;
            _Server.Events.ServerStopped += ServerStopped;

            _Server.Callbacks.SyncRequestReceived = SyncRequestReceived;
                
            // _Server.Settings.IdleClientTimeoutSeconds = 10;
            // _Server.Settings.PresharedKey = "0000000000000000";
            _Server.Settings.Logger = Logger;
            _Server.Settings.DebugMessages = _DebugMessages;
            _Server.Settings.NoDelay = true;

            _Server.Keepalive.EnableTcpKeepAlives = true;
            _Server.Keepalive.TcpKeepAliveInterval = 1;
            _Server.Keepalive.TcpKeepAliveTime = 1;
            _Server.Keepalive.TcpKeepAliveRetryCount = 3;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return;
        }
             
        _Server.Start();

        bool runForever = true;
        List<string> clients;
        string ipPort;
        MessageStatus reason = MessageStatus.Removed;
        Dictionary<object, object> metadata;
        bool success = false;

        while (runForever)
        {
            string userInput = InputString("Command [? for help]:", null, false);
                 
            switch (userInput)
            {
                case "?":
                    bool listening = (_Server != null ? _Server.IsListening : false);
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("  ?                   help (this menu)");
                    Console.WriteLine("  q                   quit");
                    Console.WriteLine("  cls                 clear screen");
                    Console.WriteLine("  start               start listening for connections (listening: " + listening.ToString() + ")");
                    Console.WriteLine("  stop                stop listening for connections  (listening: " + listening.ToString() + ")");
                    Console.WriteLine("  list                list clients");
                    Console.WriteLine("  dispose             dispose of the server");
                    Console.WriteLine("  send                send message to client");
                    Console.WriteLine("  send offset         send message to client with offset");
                    Console.WriteLine("  send md             send message with metadata to client");
                    Console.WriteLine("  sendasync           send message to a client asynchronously");
                    Console.WriteLine("  sendasync md        send message with metadata to a client asynchronously");
                    Console.WriteLine("  sendandwait         send message and wait for a response");
                    Console.WriteLine("  sendempty           send empty message with metadata");
                    Console.WriteLine("  sendandwait empty   send empty message with metadata and wait for a response");
                    Console.WriteLine("  remove              disconnect client");
                    Console.WriteLine("  remove all          disconnect all clients");
                    Console.WriteLine("  psk                 set preshared key");
                    Console.WriteLine("  stats               display server statistics");
                    Console.WriteLine("  stats reset         reset statistics other than start time and uptime"); 
                    Console.WriteLine("  debug               enable/disable debug");
                    break;

                case "q":
                    runForever = false;
                    break;

                case "cls":
                    Console.Clear();
                    break;

                case "start":
                    _Server.Start();
                    break;

                case "stop":
                    _Server.Stop();
                    break;

                case "list":
                    clients = _Server.ListClients().ToList();
                    if (clients != null && clients.Count > 0)
                    {
                        Console.WriteLine("Clients");
                        foreach (string curr in clients)
                        {
                            Console.WriteLine("  " + curr);
                        }
                    }
                    else
                    {
                        Console.WriteLine("None");
                    }
                    break;

                case "dispose":
                    _Server.Dispose();
                    break;

                case "send":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    userInput = InputString("Data:", null, false);
                    if (!_Server.Send(ipPort, userInput)) Console.WriteLine("Failed");
                    break;

                case "send offset":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    userInput = InputString("Data:", null, false);
                    int offset = InputInteger("Offset:", 0, true, true);
                    if (!_Server.Send(ipPort, Encoding.UTF8.GetBytes(userInput), null, offset)) Console.WriteLine("Failed");
                    break;

                case "send10":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    userInput = InputString("Data:", null, false);
                    for (int i = 0; i < 10; i++)
                    {
                        Console.WriteLine("Sending " + i);
                        if (!_Server.Send(ipPort, userInput + "[" + i.ToString() + "]")) Console.WriteLine("Failed");
                    }
                    break;

                case "send md":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    userInput = InputString("Data:", null, false);
                    metadata = InputDictionary();
                    if (!_Server.Send(ipPort, userInput, metadata)) Console.WriteLine("Failed"); 
                    break;

                case "send md large":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    metadata = new Dictionary<object, object>();
                    for (int i = 0; i < 100000; i++) metadata.Add(i, i);
                    if (!_Server.Send(ipPort, "Hello!", metadata)) Console.WriteLine("Failed");
                    break;

                case "sendasync":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    userInput = InputString("Data:", null, false); 
                    success = _Server.SendAsync(ipPort, Encoding.UTF8.GetBytes(userInput)).Result;
                    if (!success) Console.WriteLine("Failed");
                    break;

                case "sendasync md":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    userInput = InputString("Data:", null, false);
                    metadata = InputDictionary();
                    success = _Server.SendAsync(ipPort, Encoding.UTF8.GetBytes(userInput), metadata).Result;
                    if (!success) Console.WriteLine("Failed");
                    break;

                case "sendandwait":
                    SendAndWait();
                    break;

                case "sendempty":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    metadata = InputDictionary();
                    if (!_Server.Send(ipPort, "", metadata)) Console.WriteLine("Failed");
                    break;

                case "sendandwait empty":
                    SendAndWaitEmpty();
                    break;

                case "remove":
                    ipPort = InputString("IP:port:", _LastIpPort, false);
                    Console.WriteLine("Valid disconnect reasons: Removed, Normal, Shutdown, Timeout");
                    reason = (MessageStatus)(Enum.Parse(typeof(MessageStatus), InputString("Disconnect reason:", "Removed", false)));
                    _Server.DisconnectClient(ipPort, reason);
                    break;

                case "remove all":
                    _Server.DisconnectClients();
                    break;

                case "psk":
                    _Server.Settings.PresharedKey = InputString("Preshared key:", "1234567812345678", false);
                    break;

                case "stats":
                    Console.WriteLine(_Server.Statistics.ToString());
                    break;

                case "stats reset":
                    _Server.Statistics.Reset();
                    break;
                         
                case "debug":
                    _Server.Settings.DebugMessages = !_Server.Settings.DebugMessages;
                    Console.WriteLine("Debug set to: " + _Server.Settings.DebugMessages);
                    break;
                         
                default:
                    break;
            }
        }
    }
    
    private static string GetLastClient()
    {
        var clients = _Server.ListClients();
        if (!clients.Any()) return string.Empty;
        else
        {
            return clients.Last();
        }
    }

    private static bool InputBoolean(string question, bool yesDefault)
    {
        Console.Write(question);

        if (yesDefault) Console.Write(" [Y/n]? ");
        else Console.Write(" [y/N]? ");

        string userInput = Console.ReadLine();

        if (String.IsNullOrEmpty(userInput))
        {
            if (yesDefault) return true;
            return false;
        }

        userInput = userInput.ToLower();

        if (yesDefault)
        {
            if (
                (String.Compare(userInput, "n") == 0)
                || (String.Compare(userInput, "no") == 0)
               )
            {
                return false;
            }

            return true;
        }
        else
        {
            if (
                (String.Compare(userInput, "y") == 0)
                || (String.Compare(userInput, "yes") == 0)
               )
            {
                return true;
            }

            return false;
        }
    }

    private static string InputString(string question, string defaultAnswer, bool allowNull)
    {
        while (true)
        {
            Console.Write(question);

            if (!String.IsNullOrEmpty(defaultAnswer))
            {
                Console.Write(" [" + defaultAnswer + "]");
            }

            Console.Write(" ");

            string userInput = Console.ReadLine();

            if (String.IsNullOrEmpty(userInput))
            {
                if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                if (allowNull) return null;
                else continue;
            }

            return userInput;
        }
    }

    private static int InputInteger(string question, int defaultAnswer, bool positiveOnly, bool allowZero)
    {
        while (true)
        {
            Console.Write(question);
            Console.Write(" [" + defaultAnswer + "] ");

            string userInput = Console.ReadLine();

            if (String.IsNullOrEmpty(userInput))
            {
                return defaultAnswer;
            }

            int ret = 0;
            if (!Int32.TryParse(userInput, out ret))
            {
                Console.WriteLine("Please enter a valid integer.");
                continue;
            }

            if (ret == 0)
            {
                if (allowZero)
                {
                    return 0;
                }
            }

            if (ret < 0)
            {
                if (positiveOnly)
                {
                    Console.WriteLine("Please enter a value greater than zero.");
                    continue;
                }
            }

            return ret;
        }
    }

    private static Dictionary<object, object> InputDictionary()
    {
        Console.WriteLine("Build metadata, press ENTER on 'Key' to exit");

        Dictionary<object, object> ret = new Dictionary<object, object>();

        while (true)
        {
            Console.Write("Key   : ");
            string key = Console.ReadLine();
            if (String.IsNullOrEmpty(key)) return ret;

            Console.Write("Value : ");
            string val = Console.ReadLine();
            ret.Add(key, val);
        }
    }
         
    private static void ClientConnected(object sender, ConnectionEventArgs args)
    {
        _LastIpPort = args.IpPort;
        Console.WriteLine("Client connected: " + args.IpPort);

        _Server.Send(args.IpPort, $"Connected to server as {args.IpPort}");
        // Console.WriteLine("Disconnecting: " + args.IpPort);
        // server.DisconnectClient(args.IpPort);
    }
         
    private static void ClientDisconnected(object sender, DisconnectionEventArgs args)
    {
        _LastIpPort = GetLastClient();
        
        Console.WriteLine("Client disconnected: " + args.IpPort + ": " + args.Reason.ToString());
        
        //-- Doodlenite --
        //if a client who was waiting for a new server disconnected, forget this client
        if (clientsWaitingForNewServer.Contains(args.IpPort))
        {
            clientsWaitingForNewServer.Remove(args.IpPort);
            return;
        }

        //if a doodlenite server disconnected
        var serverInfo = ServerInfo.GetServerInfoByIpPort(args.IpPort);

        //do nothing if it wasn't a server who disconnected
        if (serverInfo == null) return;
        
        //if a server disconnected, remove the serverInfo
        Console.WriteLine($"Remove server {serverInfo.IpPort}, lobbyCode: {serverInfo.LobbyCode}");
        ServerInfo.RemoveServer(serverInfo.LobbyCode);
    }
         
    private static void MessageReceived(object sender, MessageReceivedEventArgs args)
    {
        _LastIpPort = args.IpPort;
        Console.Write("Message from " + args.IpPort + ": ");
        string receivedString = (args.Data != null) ? Encoding.UTF8.GetString(args.Data) : "[null]";
        Console.WriteLine(receivedString);

        if (args.Metadata != null && args.Metadata.Count > 0)
        {
            Console.WriteLine("Metadata:");
            foreach (KeyValuePair<object, object> curr in args.Metadata)
            {
                Console.WriteLine("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
            }
        }
        
        //-- Doodlenite --

        //start Doodlenite server
        if (receivedString == "host")
        {
            //add client to waiting list, in order to inform them about the server once it's running
            clientsWaitingForNewServer.Add(args.IpPort);
            
            //generate new serverInfo
            string lobbyCode = LobbyCodes.GenerateNew();
            ushort port = Ports.ProvideNewPort();
            //here we should check if port==0. This would mean that no more ports are available and we can't host a new game.
            var serverInfo = new ServerInfo(lobbyCode, port);
            
            //start doodlenite server
            var serverProcess = new Process();
            var startInfo = new ProcessStartInfo
            {
                FileName = serverExePath,
                Arguments = $"port={serverInfo.ServerPort} lobbyCode={serverInfo.LobbyCode}",
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                UseShellExecute = false,
                CreateNoWindow = false
            };
            serverProcess.StartInfo = startInfo;

            ThreadStart ths = new ThreadStart(() => serverProcess.Start());
            Thread th = new Thread(ths);
            th.Start();

            //update server status
            serverInfo.Status = ServerInfo.ServerStatus.Starting;

            //from here on we wait for the server to start and if ready send a message with "serverStarted" and the lobbyCode
        }
        
        //join Doodlenite server
        if (receivedString == "join") // + metadata: lobby code
        {
            string lobbyCode = (string)args.Metadata["lobbyCode"];
            if (ServerInfo.ServerExists(lobbyCode))
            {
                if (ServerInfo.ServerIsInLobby(lobbyCode))
                {
                    //send port to client so that they can join
                    var serverInfo = ServerInfo.GetServerInfoByLobbyCode(lobbyCode);
                    var metadata = new Dictionary<object, object>();
                    metadata.Add("port", serverInfo.ServerPort.ToString());
                    metadata.Add("lobbyCode", lobbyCode);
                    _Server.Send(args.IpPort, "clientCanJoin", metadata);
                }
                //if the game has already started and isn't in the lobby anymore
                else
                {
                    var metadata = new Dictionary<object, object>();
                    metadata.Add("reason", $"The game {lobbyCode} has already started. Can't late join!");
                    _Server.Send(args.IpPort, "lobbyJoinFailed", metadata);
                }
            }
            //inform client that no matching lobby was found
            else
            {
                var metadata = new Dictionary<object, object>();
                metadata.Add("reason", $"No matching lobby for code {lobbyCode} was found!");
                _Server.Send(args.IpPort, "lobbyJoinFailed", metadata);
            }
        }
        
        //doodlenite server started and is ready for clients to join
        if (receivedString == "serverStarted") // + metadata: lobby code
        {
            string lobbyCode = (string)args.Metadata["lobbyCode"];
            var serverInfo = ServerInfo.GetServerInfoByLobbyCode(lobbyCode);
            
            //update server info
            serverInfo.Status = ServerInfo.ServerStatus.InLobby;
            serverInfo.IpPort = args.IpPort;
            
            //inform waiting clients about running server
            if (clientsWaitingForNewServer.Count > 0)
            {
                //inform the first waiting client about the newly started server, in order for them to join
                var metadata = new Dictionary<object, object>();
                metadata.Add("port", serverInfo.ServerPort.ToString());
                metadata.Add("lobbyCode", lobbyCode);
                _Server.Send(clientsWaitingForNewServer[0], "clientCanJoin", metadata);
                
                //remove the client from the waiting list
                clientsWaitingForNewServer.RemoveAt(0);
            }
        }
        
        //doodlenite server switched from lobby to the game
        if (receivedString == "serverInGame") // + metadata: lobby code
        {
            string lobbyCode = (string)args.Metadata["lobbyCode"];
            if(ServerInfo.GetServerInfoByLobbyCode(lobbyCode) != null)
                ServerInfo.GetServerInfoByLobbyCode(lobbyCode).Status = ServerInfo.ServerStatus.InGame;
        }

        //doodlenite server stopped
        if (receivedString == "serverStopped") // + metadata: lobby code
        {
            //string lobbyCode = (string)args.Metadata["lobbyCode"];
            //ServerInfo.GetServerInfoByLobbyCode(lobbyCode).Status = ServerInfo.ServerStatus.Offline;
        }
        //
    }

    private static void ServerStarted(object sender, EventArgs args)
    {
        Console.WriteLine("Server started");
    }

    private static void ServerStopped(object sender, EventArgs args)
    {
        Console.WriteLine("Server stopped");
    }

    private static SyncResponse SyncRequestReceived(SyncRequest req)
    {
        Console.Write("Synchronous request received from " + req.IpPort + ": ");
        if (req.Data != null) Console.WriteLine(Encoding.UTF8.GetString(req.Data));
        else Console.WriteLine("[null]");

        if (req.Metadata != null && req.Metadata.Count > 0)
        {
            Console.WriteLine("Metadata:");
            foreach (KeyValuePair<object, object> curr in req.Metadata)
            {
                Console.WriteLine("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
            }
        }

        Dictionary<object, object> retMetadata = new Dictionary<object, object>();
        retMetadata.Add("foo", "bar");
        retMetadata.Add("bar", "baz");

        // Uncomment to test timeout
        // Task.Delay(10000).Wait();
        Console.WriteLine("Sending synchronous response");
        return new SyncResponse(req, retMetadata, "Here is your response!");
    }

    private static void SendAndWait()
    {
        string ipPort = InputString("IP:port:", _LastIpPort, false);
        string userInput = InputString("Data:", null, false);
        int timeoutMs = InputInteger("Timeout (milliseconds):", 5000, true, false);

        try
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            SyncResponse resp = _Server.SendAndWait(timeoutMs, ipPort, userInput);
            stopwatch.Stop();
            if (resp.Metadata != null && resp.Metadata.Count > 0)
            {
                Console.WriteLine("Metadata:");
                foreach (KeyValuePair<object, object> curr in resp.Metadata)
                {
                    Console.WriteLine("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
                }
            }

            Console.WriteLine("Response: " + Encoding.UTF8.GetString(resp.Data));
            Console.WriteLine("Client responded in {0} ms/{1} ticks.", stopwatch.ElapsedMilliseconds, stopwatch.ElapsedTicks);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.ToString());
        }
    }

    private static void SendAndWaitEmpty()
    {
        string ipPort = InputString("IP:port:", _LastIpPort, false); 
        int timeoutMs = InputInteger("Timeout (milliseconds):", 5000, true, false);

        Dictionary<object, object> dict = new Dictionary<object, object>();
        dict.Add("foo", "bar");

        try
        {
            SyncResponse resp = _Server.SendAndWait(timeoutMs, ipPort, "", dict);
            if (resp.Metadata != null && resp.Metadata.Count > 0)
            {
                Console.WriteLine("Metadata:");
                foreach (KeyValuePair<object, object> curr in resp.Metadata)
                {
                    Console.WriteLine("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
                }
            }

            Console.WriteLine("Response: " + Encoding.UTF8.GetString(resp.Data));
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.ToString());
        }
    }

    private static void Logger(Severity sev, string msg)
    {
        Console.WriteLine("[" + sev.ToString().PadRight(9) + "] " + msg);
    }
}
}