namespace FutbolSitesi.Models
{
    public class Standing
    {
        public int Id { get; set; }

        public string Season { get; set; } = string.Empty;
        public string League { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;

        public int Played { get; set; }
        public int Win { get; set; }
        public int Draw { get; set; }
        public int Lose { get; set; }

        public int GoalFor { get; set; }
        public int GoalAgainst { get; set; }
        public int GoalDiff { get; set; }

        public int Points { get; set; }
    }
}

