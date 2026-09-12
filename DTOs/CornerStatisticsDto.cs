namespace FutbolSitesi.DTOs
{
    public class CornerStatisticsDto
    {
        public string Team { get; set; } = string.Empty;
        public int CornersFor { get; set; }
        public int CornersAgainst { get; set; }
        public int HomeMatchCount { get; set; }
        public int AwayMatchCount { get; set; }
        public int MatchCount { get; set; }
        public int TotalMatchCorners { get; set; }
        public int Over75Count { get; set; }
        public int Over85Count { get; set; }
        public int Over95Count { get; set; }
        public int Over105Count { get; set; }
        public int HomeOver35Count { get; set; }
        public int AwayOver35Count { get; set; }
        public int HomeOver45Count { get; set; }
        public int AwayOver45Count { get; set; }
        public int HomeOver55Count { get; set; }
        public int AwayOver55Count { get; set; }
        public int HomeOver65Count { get; set; }
        public int AwayOver65Count { get; set; }
        public int HomeOver75Count { get; set; }
        public int AwayOver75Count { get; set; }
        public int HomeOver85Count { get; set; }
        public int AwayOver85Count { get; set; }
        public double AvgTeamCorners { get; set; }
        public double AvgMatchCorners { get; set; }
        public double Over75Rate { get; set; }
        public double Over85Rate { get; set; }
        public double Over95Rate { get; set; }
        public double Over105Rate { get; set; }
        public double Team35Rate { get; set; }
        public double Team45Rate { get; set; }
        public double Team55Rate { get; set; }
        public double Team65Rate { get; set; }
        public double Team75Rate { get; set; }
        public double Team85Rate { get; set; }
        public double AvgCornersUsed { get; set; }
        public int Rank { get; set; }
    }
}