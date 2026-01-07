namespace FutbolSitesi.Models
{
    public class Match
    {
        public int Id { get; set; }

        public string Season { get; set; } = string.Empty;
        public string League { get; set; } = string.Empty;
        public int Week { get; set; }

        public DateTime Date { get; set; }   // 🟢 MAÇ TARİHİ (UTC)
        public string Time { get; set; } = string.Empty; // HH:mm

        public string HomeTeam { get; set; } = string.Empty;
        public string AwayTeam { get; set; } = string.Empty;

        public string Winner { get; set; } = string.Empty; // Home | Away | Draw | TBD

        public int GoalHome { get; set; }
        public int GoalAway { get; set; }

        public int CornerHome { get; set; }
        public int CornerAway { get; set; }

        public int YellowHome { get; set; }
        public int YellowAway { get; set; }

        public int RedHome { get; set; }
        public int RedAway { get; set; }

        /* ----------------- YENİ EKLENENLER ----------------- */

        // Şutlar
        public int ShotsHome { get; set; }
        public int ShotsAway { get; set; }

        // İsabetli şutlar
        public int ShotsOnTargetHome { get; set; }
        public int ShotsOnTargetAway { get; set; }

        // Fauller
        public int FoulsHome { get; set; }
        public int FoulsAway { get; set; }

        // Topa sahip olma (%)
        public int PossessionHome { get; set; }
        public int PossessionAway { get; set; }

        // Gol dakikaları (örn: "12|45+1|78")
        public string? HomeGoalsMinutes { get; set; } // 🔥 NULL olabilir
        public string? AwayGoalsMinutes { get; set; } // 🔥 NULL olabilir
    }
}
