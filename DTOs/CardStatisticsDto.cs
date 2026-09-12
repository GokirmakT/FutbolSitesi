namespace FutbolSitesi.DTOs
{
    public class CardStatisticsDto
    {
        public string Team { get; set; } = string.Empty;
        public int YellowCards { get; set; }
        public int RedCards { get; set; }
        public int MatchCount { get; set; }
        public int TotalMatchCards { get; set; }
        public int OppYellow { get; set; }
        public int OppRed { get; set; }
        public int Over25Count { get; set; }
        public int Over35Count { get; set; }
        public int Over45Count { get; set; }
        public int Over55Count { get; set; }
        public int RedOver05Count { get; set; }
        public int RedOver15Count { get; set; }
        public int RedOver25Count { get; set; }
        public int PenaltyOver25Count { get; set; }
        public int PenaltyOver35Count { get; set; }
        public int PenaltyOver45Count { get; set; }
        public int PenaltyOver55Count { get; set; }
        public double CardScore { get; set; }
        public double AvgCardScore { get; set; }
        public int TotalCardCount { get; set; }
        public double AvgCardCount { get; set; }
        public double AvgMatchCards { get; set; }
        public int OwnPenalty { get; set; }
        public int OppPenalty { get; set; }
        public int TotalRedCards { get; set; }
        public double Over25Rate { get; set; }
        public double Over35Rate { get; set; }
        public double Over45Rate { get; set; }
        public double Over55Rate { get; set; }
        public double RedOver05Rate { get; set; }
        public double RedOver15Rate { get; set; }
        public double RedOver25Rate { get; set; }
        public double PenaltyOver25Rate { get; set; }
        public double PenaltyOver35Rate { get; set; }
        public double PenaltyOver45Rate { get; set; }
        public double PenaltyOver55Rate { get; set; }
        public int Rank { get; set; }
    }
}