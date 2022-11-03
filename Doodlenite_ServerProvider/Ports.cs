//(c) copyright by Martin M. Klöckener
namespace DoodleniteServerProvider;

public static class Ports
{
    private static ushort portRangeLow = 50881; //inclusive
    private static ushort portRangeHigh = 50890; //inclusive

    private static List<ushort> ports = new List<ushort>();

    public static ushort ProvideNewPort()
    {
        ushort port = 0;
        
        for (ushort i = portRangeLow; i < portRangeHigh+1; i++)
        {
            if (!ports.Contains(i))
            {
                port = i;
                ports.Add(port);
                return port;
            }
        }
        
        //returns 0 if all ports are in use
        return port;
    }

    public static void MakePortAvailableAgain(ushort _port)
    {
        if (ports.Contains(_port)) ports.Remove(_port);
    }
}