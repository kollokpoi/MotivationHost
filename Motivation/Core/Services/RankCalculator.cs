namespace Motivation.Core.Services
{
    public class RankCalculator
    {
        private const int PlusRankLimit = 1;
        private const int MinusRankLimit = -2;
        public const int BasePoints = 119;
        private const int MaxScore = 110;

        private static readonly List<int> RankRanges = new()
        {
            10,
            20,
            30,
            40,
            50,
            60,
            70,
            80,
            90,
            100,
            MaxScore,
        };

        public int CalculateScore(int penaltyPoints, int qualificationPoints) =>
            BasePoints + qualificationPoints - penaltyPoints;

        // DO I NEED TO STORE THE POINTS SOMEWHERE LIKE THE ACTUAL POINTS???

        public int CalculateRankNumber(int score)
        {
            var rankNumber = 0;
            while (score > RankRanges[rankNumber] && score < MaxScore)
                rankNumber++;
            if (score > RankRanges.Last())
            {
                rankNumber = RankRanges.Count() - 1; // get the last rank
            }
            return rankNumber + 1;
        }

        public int CalculateNewRankWithLimits(int oldRank, int newRank)
        {
            var rankDifference = Math.Clamp(newRank - oldRank, MinusRankLimit, PlusRankLimit);
            return oldRank + rankDifference;
        }
    }
}
