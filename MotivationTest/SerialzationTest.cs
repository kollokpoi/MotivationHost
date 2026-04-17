using Motivation.Models.Mobile;
using Newtonsoft.Json;

namespace MotivationTest
{
    public class SerialzationTest
    {
        [Fact]
        public void CardSerializationTest()
        {
            var card = new Card
            {
                Name = "nextRank",
                Title = "Следующий Ранг",
                Content = "5",
                SortIndex = 1
            };

            var json = JsonConvert.SerializeObject(card);
            Assert.DoesNotContain("highlited", json);
        }
    }
}