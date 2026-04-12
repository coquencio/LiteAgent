using LiteAgent.Tooling;

namespace ChatConsoleApp;
public class InventoryPlugins
{
    [LitePlugin("Gets inventory from person's last name")]
    public List<string> GetInventory(string lastName)
    {
        Console.WriteLine(lastName);
        return new List<string>
        {
            "PC",
            "Table",
            "Chair",
        };
    }
}
