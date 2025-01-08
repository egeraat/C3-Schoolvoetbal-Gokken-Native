public class Bet
{
    public int BetId { get; set; }
    public string TeamA { get; set; }
    public string TeamB { get; set; }
    public string BetOnTeam { get; set; }
    public decimal BetAmount { get; set; }
    public bool? HasWon { get; set; }
    public int MatchId { get; set; }
}
