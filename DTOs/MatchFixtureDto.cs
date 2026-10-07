namespace FutbolSitesi.DTOs
{
    public sealed record MatchFixtureDto(
        int Id,
        string Season,
        string League,
        int Week,
        DateTime Date,
        string Time,
        string HomeTeam,
        string AwayTeam,
        string Winner,
        int GoalHome,
        int GoalAway
    );
}
