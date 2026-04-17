using Motivation.Options;

namespace Motivation.Data.Repositories
{
    public class BitrixBaseRepository
    {
        protected readonly IConfiguration _configuration;
        protected readonly HttpClient _httpClient;

        public HttpClient Client
        {
            get => _httpClient;
        }

        public BitrixBaseRepository(IConfiguration configuration)
        {
            _configuration = configuration;

            var opts = _configuration.Get<BitrixOptions>();
            _httpClient = new HttpClient { BaseAddress = new Uri(opts!.BitrixBridgeAppURL) };
        }
    }
}
