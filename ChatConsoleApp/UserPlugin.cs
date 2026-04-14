using LiteAgent.Tooling;

namespace ChatConsoleApp
{
    internal class UserPlugin
    {
        [LitePlugin("Gets user details")]
        public UserInfo GetUserDetailsByUserName(string userName)
        {
            return new UserInfo(1, userName, "johndoe@mail.com");
        }
    }   

    public record UserInfo(int Id, string Name, string Email);
}
