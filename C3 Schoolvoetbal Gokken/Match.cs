public class Match
{
    public int Id { get; set; }  // Add this to represent the match ID.
    public string GameId { get; set; }
    public string Team1Id { get; set; }
    public string Team2Id { get; set; }
    public string Status { get; set; }
    public string Field { get; set; }
    public string Uitslag { get; set; }
    public string CreatedAt { get; set; }

    public string TeamA { get; set; }
    public string TeamB { get; set; }
}
