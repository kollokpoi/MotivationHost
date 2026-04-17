namespace Motivation.Options
{
    public class PostgresOptions
    {
        [ConfigurationKeyName("POSTGRES_SERVER")]
        public string PostgresServer { get; set; } = "192.168.0.54";

        [ConfigurationKeyName("POSTGRES_PORT")]
        public string PostgresPort { get; set; } = "5432";

        [ConfigurationKeyName("POSTGRES_USER")]
        public string PostgresUser { get; set; } = "bg-admin-postgres";

        [ConfigurationKeyName("POSTGRES_PASSWORD")]
        public string PostgresPassword { get; set; } = "kKtkv3C8A278FR";

        [ConfigurationKeyName("POSTGRES_APP_DATABASE_NAME")]
        public string AppDatabaseName { get; set; } = "appdatabase";

        [ConfigurationKeyName("POSTGRES_IDENTITY_DATABASE_NAME")]
        public string IdentityDatabaseName { get; set; } = "identitydatabase";
    }
}
