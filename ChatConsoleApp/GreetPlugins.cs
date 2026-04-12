using LiteAgent.Tooling;

namespace ChatConsoleApp
{
    internal class GreetPlugins
    {
        [LitePlugin("Get's person's last name")]
        public string GetPersonLastName(string firstName)
        {
            return "Smith";
        }
    }
}
