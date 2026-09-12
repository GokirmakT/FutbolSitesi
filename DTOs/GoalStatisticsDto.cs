namespace FutbolSitesi.DTOs
{
    public class GoalStatisticsDto
    {
        public string Team { get; set; } = string.Empty;
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int HomeMatchCount { get; set; }
        public int AwayMatchCount { get; set; }
        public int MatchCount { get; set; }
        public int TotalMatchGoals { get; set; }
        public int Bts { get; set; }
        public int HomeBts { get; set; }
        public int AwayBts { get; set; }
        public int BothHalvesScored { get; set; }
        public int HomeBothHalvesScored { get; set; }
        public int AwayBothHalvesScored { get; set; }
        public int Over25Count { get; set; }
        public int HomeOver25Count { get; set; }
        public int AwayOver25Count { get; set; }
        public int Over35Count { get; set; }
        public int HomeOver35Count { get; set; }
        public int AwayOver35Count { get; set; }
        public int Over45Count { get; set; }
        public int HomeOver45Count { get; set; }
        public int AwayOver45Count { get; set; }
        public int Over15Count { get; set; }
        public int HomeOver15Count { get; set; }
        public int AwayOver15Count { get; set; }
        public int Less25Count { get; set; }
        public int HomeLess25Count { get; set; }
        public int AwayLess25Count { get; set; }
        public int Less35Count { get; set; }
        public int HomeLess35Count { get; set; }
        public int AwayLess35Count { get; set; }
        public int Less45Count { get; set; }
        public int HomeLess45Count { get; set; }
        public int AwayLess45Count { get; set; }
        public int Less15Count { get; set; }
        public int HomeLess15Count { get; set; }
        public int AwayLess15Count { get; set; }
        public double AvgGoalsFor { get; set; }
        public double AvgMatchGoals { get; set; }
        public double BtsRate { get; set; }
        public double HomeBtsRate { get; set; }
        public double AwayBtsRate { get; set; }
        public double Over25Rate { get; set; }
        public double Over35Rate { get; set; }
        public double Over45Rate { get; set; }
        public double Over15Rate { get; set; }
        public double Less25Rate { get; set; }
        public double Less35Rate { get; set; }
        public double Less45Rate { get; set; }
        public double Less15Rate { get; set; }
        public double HomeOver25Rate { get; set; }
        public double AwayOver25Rate { get; set; }
        public double HomeOver35Rate { get; set; }
        public double AwayOver35Rate { get; set; }
        public double HomeOver45Rate { get; set; }
        public double AwayOver45Rate { get; set; }
        public double HomeOver15Rate { get; set; }
        public double AwayOver15Rate { get; set; }
        public double HomeLess25Rate { get; set; }
        public double AwayLess25Rate { get; set; }
        public double HomeLess35Rate { get; set; }
        public double AwayLess35Rate { get; set; }
        public double HomeLess45Rate { get; set; }
        public double AwayLess45Rate { get; set; }
        public double HomeLess15Rate { get; set; }
        public double AwayLess15Rate { get; set; }
        public double BothHalvesRate { get; set; }
        public double HomeBothHalvesRate { get; set; }
        public double AwayBothHalvesRate { get; set; }
        public int Rank { get; set; }
    }
}