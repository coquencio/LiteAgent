using LiteAgent.Tooling;

namespace ChatConsoleApp
{
    internal class GreetPlugins : LitePluginBase
    {
        [LitePlugin("Sends a greet asking for the name of the person")]
        public string Greet(string name)
        {
            Console.WriteLine($"Hello, {name}! this is the actual method run");
            return "Greet method executed successfully.";
        }
    }
}
