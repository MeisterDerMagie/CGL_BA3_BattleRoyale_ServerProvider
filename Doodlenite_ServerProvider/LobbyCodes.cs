//(c) copyright by Martin M. Klöckener

namespace DoodleniteServerProvider;

public static class LobbyCodes
{
    private static List<string> codes = new List<string>();
    
    private static char[] charPool = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
    private static int length = 5;

    public static string GenerateNew()
    {
        string newCode = string.Empty;
        var rand = new Random();
        
        for (int i = 0; i < length; i++)
        {
            char randomChar = charPool[rand.NextInt64(charPool.Length)];
            newCode += randomChar;
        }
        
        //make sure the new code is unique
        if (codes.Contains(newCode)) newCode = GenerateNew();

        //add to the list
        codes.Add(newCode);

        return newCode;
    }

    public static void RemoveLobbyCode(string _code)
    {
        if (codes.Contains(_code)) codes.Remove(_code);
    }
}