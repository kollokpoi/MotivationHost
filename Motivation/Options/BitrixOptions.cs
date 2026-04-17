namespace Motivation.Options
{
    public class BitrixOptions
    {
        [ConfigurationKeyName("BITRIX_PORTAL_HOST")]
        public string BitrixPortalHost { get; set; } = string.Empty;

        [ConfigurationKeyName("BITRIX_BRIDGE_APP_URL")]
        public string BitrixBridgeAppURL { get; set; } = string.Empty;
    }
}
