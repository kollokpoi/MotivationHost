using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Motivation.Options
{
    public class AuthOptions
    {
        public const string Issuer = "BackgroundAdminServer";
        public const string Audience = "BackgroundAdminClient";
        private const string AccessPrivateKey =
            "$6Yexrparb5nfO7o%9*30^oR6O3&m0Xu&7J@O&y$NvWuCn*FqM#6@h2M&6&^Ud^kK8*7z9kn89^";
        private const string RefreshPrivateKey =
            "*FqM#6@h2M&6&^Ud^$6Yexarb5nrpkK8*7z9kn89^fO7o%9*33&m0Xu&0^oR6&y$NvWO7J@OuCn";
        public const int LifetimeMinutes = 3600;
        public const int RefreshLifetime = 60 * 60 * 24 * 180;

        public static SymmetricSecurityKey GetSymmetricAccessSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(AccessPrivateKey));
        }

        public static SymmetricSecurityKey GetSymmetricRefreshSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(RefreshPrivateKey));
        }
    }
}
