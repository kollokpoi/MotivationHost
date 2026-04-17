namespace Motivation.Data.Repositories
{
    public class BitrixTimemanRepository : BitrixBaseRepository
    {
        private readonly IEmployeesRepository _employeesRepository;

        public BitrixTimemanRepository(
            IConfiguration configuration,
            IEmployeesRepository employeesRepository
        )
            : base(configuration)
        {
            _employeesRepository = employeesRepository;
        }

        public async Task Do(string action, int userId)
        {
            var res = await _httpClient.PostAsync(
                $"api/timeman/do.php?action={action}&userId={userId}",
                null
            );
            if (res.IsSuccessStatusCode)
            {
                Console.WriteLine(await res.Content.ReadAsStringAsync());
            }

            res.EnsureSuccessStatusCode();
        }
    }
}
