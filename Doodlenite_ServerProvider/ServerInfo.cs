//(c) copyright by Martin M. Klöckener
namespace DoodleniteServerProvider;

public class ServerInfo
{
    private static List<ServerInfo?> servers = new List<ServerInfo?>();

    public string LobbyCode { get; private set; }
    public string IpPort;
    public ushort ServerPort { get; private set; }
    public ServerStatus Status { get; set; }

    public ServerInfo(string _lobbyCode, ushort _serverPort, ServerStatus _status = ServerStatus.Offline)
    {
        LobbyCode = _lobbyCode;
        IpPort = string.Empty;
        ServerPort = _serverPort;
        Status = _status;
        
        servers.Add(this);
    }
    
    public static ServerInfo? GetServerInfoByLobbyCode(string _lobbyCode)
    {
        foreach (ServerInfo? serverInfo in servers)
        {
            if (serverInfo.LobbyCode == _lobbyCode) return serverInfo;
        }

        return null;
    }

    public static ServerInfo? GetServerInfoByIpPort(string _ipPort)
    {
        foreach (ServerInfo? serverInfo in servers)
        {
            if (serverInfo.IpPort == _ipPort) return serverInfo;
        }

        return null;
    }

    public static bool ServerExists(string _lobbyCode)
    {
        foreach (ServerInfo? serverInfo in servers)
        {
            if (serverInfo.LobbyCode == _lobbyCode) return true;
        }

        return false;
    }

    public static bool ServerIsInLobby(string _lobbyCode)
    {
        foreach (ServerInfo? serverInfo in servers)
        {
            if (serverInfo.LobbyCode == _lobbyCode && serverInfo.Status == ServerStatus.InLobby) return true;
        }

        return false;
    }

    public static void RemoveServer(string _lobbyCode)
    {
        if (!ServerExists(_lobbyCode)) return;

        ServerInfo? toRemove = null;
        
        foreach (ServerInfo? serverInfo in servers)
        {
            if (serverInfo.LobbyCode == _lobbyCode)
            {
                Ports.MakePortAvailableAgain(serverInfo.ServerPort);
                toRemove = serverInfo;
            }
        }

        if (toRemove != null) servers.Remove(toRemove);
    }

    public static void RemoveServer(ServerInfo _serverInfo)
    {
        if (servers.Contains(_serverInfo))
        {
            Ports.MakePortAvailableAgain(_serverInfo.ServerPort);
            servers.Remove(_serverInfo);
        }
    }

    public enum ServerStatus
    {
        Offline,
        Starting,
        InLobby,
        InGame
    }
}