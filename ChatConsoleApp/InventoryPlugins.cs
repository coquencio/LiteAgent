using LiteAgent.Tooling;

namespace ChatConsoleApp;
public class InventoryPlugins : LitePluginBase
{
    [LitePlugin("Method to get inventory list, category is required")]
    public List<string> GetInventory(string category)
    {
        return new List<string>
        {
            "PC",
            "Table",
            "Chair",
        };
    }
}
