
namespace ChatConsoleApp;
public class EmailPlugins
{
    [LitePlugin("Sends an email to a specified address with a given message", maxRetries: 2)]
    public string SendEmail(string emailAddress, string message)
    {
        return "email send to " + emailAddress + " with message: " + message;
    }
}
