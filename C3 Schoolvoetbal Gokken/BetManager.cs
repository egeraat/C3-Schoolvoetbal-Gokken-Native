using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Net.Http;
using System.Text.Json;

public class BetManager
{
    static readonly string connectionString = "Server=localhost;Database=bets_db;Uid=root;Pwd=;";

    public static void ManageBets(int userId)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("1. Plaats een weddenschap");
            Console.WriteLine("2. Bekijk weddenschappen");
            Console.WriteLine("3. Bekijk saldo");
            Console.WriteLine("4. Log uit");
            Console.WriteLine("5. Admin login");
            Console.Write("Kies een optie: ");
            string keuze = Console.ReadLine();

            switch (keuze)
            {
                case "1":
                    PlaatsWeddenschap(userId);
                    break;
                case "2":
                    BekijkWeddenschappen(userId);
                    break;
                case "3":
                    Console.WriteLine($"Je saldo is: €{GetUserBalance(userId):F2}");
                    Console.ReadKey();
                    break;
                case "4":
                    return;
                case "5":
                    AdminLogin();
                    break;
                default:
                    Console.WriteLine("Ongeldige keuze.");
                    break;
            }
        }
    }



    public static async Task FetchMatchesFromAPI()
    {
        Console.Clear();
        string apiUrl = "http://127.0.0.1:8000/C3-Schoolvoetbal/matches_api.php";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                Console.WriteLine("Bezig met ophalen van gegevens van de API...");

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var matches = JsonSerializer.Deserialize<List<Match>>(jsonResponse);

                    Console.WriteLine("\nBeschikbare wedstrijden:");
                    foreach (var match in matches)
                    {
                        Console.WriteLine($"Wedstrijd: {match.TeamA} vs {match.TeamB}");
                    }
                }
                else
                {
                    Console.WriteLine($"Fout bij ophalen van gegevens: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Er is een fout opgetreden: {ex.Message}");
            }
        }

        Console.WriteLine("\nDruk op een toets om door te gaan...");
        Console.ReadKey();
    }



    public static void BekijkWeddenschappen(int userId)
    {
        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Step 1: Fetch user's bets from the database
                var query = "SELECT b.Id, b.BetDescription, b.BetAmount, b.BetOnTeam, b.HasWon, b.MatchId, m.TeamA, m.TeamB " +
                            "FROM bets b " +
                            "JOIN matches m ON b.MatchId = m.Id " +
                            "WHERE b.UserId = @UserId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        var bets = new List<(int BetId, string Description, decimal Amount, string BetOnTeam, bool? HasWon, int MatchId, string TeamA, string TeamB)>();

                        while (reader.Read())
                        {
                            bets.Add((
                                reader.GetInt32("Id"),
                                reader.IsDBNull(reader.GetOrdinal("BetDescription")) ? "Geen beschrijving" : reader.GetString("BetDescription"),
                                reader.IsDBNull(reader.GetOrdinal("BetAmount")) ? 0 : reader.GetDecimal("BetAmount"),
                                reader.IsDBNull(reader.GetOrdinal("BetOnTeam")) ? "Geen team" : reader.GetString("BetOnTeam"),
                                reader.IsDBNull(reader.GetOrdinal("HasWon")) ? (bool?)null : reader.GetBoolean("HasWon"),
                                reader.IsDBNull(reader.GetOrdinal("MatchId")) ? -1 : reader.GetInt32("MatchId"),
                                reader.IsDBNull(reader.GetOrdinal("TeamA")) ? "Onbekend" : reader.GetString("TeamA"),
                                reader.IsDBNull(reader.GetOrdinal("TeamB")) ? "Onbekend" : reader.GetString("TeamB")
                            ));
                        }

                        reader.Close();

                        // Step 2: Fetch match results from the API
                        var httpClient = new HttpClient();
                        var apiResponse = httpClient.GetStringAsync("http://127.0.0.1/C3-Schoolvoetbal/matches_api.php").Result; //test

                        if (string.IsNullOrWhiteSpace(apiResponse))
                        {
                            Console.WriteLine("De API heeft geen resultaten teruggestuurd.");
                            return;
                        }

                        // Parse the API JSON response
                        var apiResults = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(apiResponse);

                        if (apiResults == null)
                        {
                            Console.WriteLine("Geen geldige resultaten ontvangen van de API.");
                            return;
                        }

                        // Step 3: Display bets and whether they've been won or lost
                        Console.WriteLine("Uw weddenschappen:");
                        foreach (var bet in bets)
                        {
                            var matchResult = apiResults.FirstOrDefault(m => m.game_id != null && (int)m.game_id == bet.MatchId);
                            string resultMessage;

                            if (matchResult != null)
                            {
                                // Parse uitslag to determine the winning team
                                string uitslag = matchResult.uitslag;
                                if (string.IsNullOrWhiteSpace(uitslag))
                                {
                                    resultMessage = "Nog geen resultaat beschikbaar";
                                }
                                else
                                {
                                    var scores = uitslag.Split('-');
                                    if (scores.Length == 2 && int.TryParse(scores[0], out int team1Score) && int.TryParse(scores[1], out int team2Score))
                                    {
                                        var winningTeam = team1Score > team2Score ? bet.TeamA : (team1Score < team2Score ? bet.TeamB : "Gelijkspel");
                                        resultMessage = winningTeam == bet.BetOnTeam ? "Gewonnen ✅" : "Verloren ❌";
                                    }
                                    else
                                    {
                                        resultMessage = "Ongeldige uitslag";
                                    }
                                }
                            }
                            else
                            {
                                resultMessage = "Nog geen resultaat beschikbaar";
                            }

                            Console.WriteLine($"- Beschrijving: {bet.Description}");
                            Console.WriteLine($"  Wedbedrag: €{bet.Amount}");
                            Console.WriteLine($"  Gekozen team: {bet.BetOnTeam}");
                            Console.WriteLine($"  Uitslag: {resultMessage}\n");
                        }
                    }
                }
            }

            Console.WriteLine("Druk op Enter om terug te keren naar het menu.");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Er is een fout opgetreden:");
            Console.WriteLine(ex.Message);
            Console.WriteLine("Druk op Enter om terug te keren naar het menu.");
            Console.ReadLine();
        }
    }




    public static List<Bet> GetUserBets(int userId)
    {
        var bets = new List<Bet>();
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT BetOnTeam, BetAmount, TeamA, TeamB FROM Bets WHERE UserId = @userId;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bets.Add(new Bet
                        {
                            TeamA = reader.GetString("TeamA"),
                            TeamB = reader.GetString("TeamB"),
                            BetOnTeam = reader.GetString("BetOnTeam"),
                            BetAmount = reader.GetDecimal("BetAmount")
                        });
                    }
                }
            }
        }
        return bets;
    }

    public static decimal GetUserBalance(int userId)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Balance FROM Users WHERE Id = @userId;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                return Convert.ToDecimal(command.ExecuteScalar());
            }
        }
    }

    public static void PlaatsWeddenschap(int userId)
    {
        Console.Clear();
        var wedstrijden = GetAvailableMatches();

        if (wedstrijden.Count == 0)
        {
            Console.WriteLine("Er zijn geen beschikbare wedstrijden om op te wedden.");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Beschikbare wedstrijden:");
        for (int i = 0; i < wedstrijden.Count; i++)
        {
            Console.WriteLine($"{i + 1}: {wedstrijden[i].TeamA} vs {wedstrijden[i].TeamB}");
        }

        Console.Write("Kies het nummer van de wedstrijd: ");
        int matchChoice;
        if (!int.TryParse(Console.ReadLine(), out matchChoice) || matchChoice < 1 || matchChoice > wedstrijden.Count)
        {
            Console.WriteLine("Ongeldige keuze.");
            Console.ReadKey();
            return;
        }

        var geselecteerdeWedstrijd = wedstrijden[matchChoice - 1];

        Console.WriteLine($"Je hebt gekozen voor: {geselecteerdeWedstrijd.TeamA} vs {geselecteerdeWedstrijd.TeamB}");
        Console.Write("Op welk team wil je wedden? ");
        string betOnTeam = Console.ReadLine().Trim().ToUpper();

        if (betOnTeam != geselecteerdeWedstrijd.TeamA.ToUpper() && betOnTeam != geselecteerdeWedstrijd.TeamB.ToUpper())
        {
            Console.WriteLine($"Ongeldige keuze. Je kunt alleen op {geselecteerdeWedstrijd.TeamA} of {geselecteerdeWedstrijd.TeamB} wedden.");
            Console.ReadKey();
            return;
        }

        Console.Write("Inzet bedrag in 4S-dollars: ");
        decimal betAmount;
        if (!decimal.TryParse(Console.ReadLine(), out betAmount) || betAmount <= 0)
        {
            Console.WriteLine("Ongeldig bedrag.");
            Console.ReadKey();
            return;
        }

        decimal currentBalance = GetUserBalance(userId);
        if (currentBalance < betAmount)
        {
            Console.WriteLine("Je hebt niet genoeg saldo voor deze weddenschap.");
            Console.ReadKey();
            return;
        }

        PlaatsWeddenschapInDatabase(userId, geselecteerdeWedstrijd.Id, betOnTeam, betAmount, geselecteerdeWedstrijd.TeamA, geselecteerdeWedstrijd.TeamB);
        Console.WriteLine("Weddenschap succesvol geplaatst!");
        Console.ReadKey();
    }

    public static void PlaatsWeddenschapInDatabase(int userId, int matchId, string betOnTeam, decimal betAmount, string teamA, string teamB)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            string insertBetQuery = @"
                INSERT INTO Bets (UserId, MatchId, BetOnTeam, BetAmount, TeamA, TeamB) 
                VALUES (@userId, @matchId, @betOnTeam, @betAmount, @teamA, @teamB);";
            using (var command = new MySqlCommand(insertBetQuery, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@matchId", matchId);
                command.Parameters.AddWithValue("@betOnTeam", betOnTeam);
                command.Parameters.AddWithValue("@betAmount", betAmount);
                command.Parameters.AddWithValue("@teamA", teamA);
                command.Parameters.AddWithValue("@teamB", teamB);
                command.ExecuteNonQuery();
            }

            string updateBalanceQuery = "UPDATE Users SET Balance = Balance - @betAmount WHERE Id = @userId;";
            using (var updateCommand = new MySqlCommand(updateBalanceQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("@betAmount", betAmount);
                updateCommand.Parameters.AddWithValue("@userId", userId);
                updateCommand.ExecuteNonQuery();
            }
        }
    }

    public static void UpdateMatchResult(int matchId, string winner)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "UPDATE Matches SET Result = @winner WHERE Id = @matchId;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@winner", winner);
                command.Parameters.AddWithValue("@matchId", matchId);
                command.ExecuteNonQuery();
            }
        }

        // Na het bijwerken van de wedstrijdresultaten, controleren we of de weddenschappen gewonnen zijn
        CheckAndPayWinners(matchId, winner);
    }

    public static void CheckAndPayWinners(int matchId, string winner)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT UserId, BetAmount, BetOnTeam FROM Bets WHERE MatchId = @matchId;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@matchId", matchId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int userId = reader.GetInt32("UserId");
                        decimal betAmount = reader.GetDecimal("BetAmount");
                        string betOnTeam = reader.GetString("BetOnTeam");

                        // Als de gebruiker het juiste team heeft gekozen, verdubbelen we hun inzet
                        if (betOnTeam.Equals(winner, StringComparison.OrdinalIgnoreCase))
                        {
                            decimal winnings = betAmount * 2;
                            UpdateUserBalance(userId, winnings);
                            Console.WriteLine($"Gebruiker {userId} heeft gewonnen! Winst: €{winnings:F2}");
                        }
                    }
                }
            }
        }
    }

    public static void UpdateUserBalance(int userId, decimal amount)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "UPDATE Users SET Balance = Balance + @amount WHERE Id = @userId;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@userId", userId);
                command.ExecuteNonQuery();
            }
        }
    }

    public static List<Match> GetAvailableMatches()
    {
        var matches = new List<Match>();
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Id, TeamA, TeamB FROM Matches WHERE Result IS NULL;";
            using (var command = new MySqlCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        matches.Add(new Match
                        {
                            Id = reader.GetInt32("Id"),
                            TeamA = reader.GetString("TeamA"),
                            TeamB = reader.GetString("TeamB")
                        });
                    }
                }
            }
        }
        return matches;
    }

    public static void AdminLogin()
    {
        Console.Clear();
        Console.Write("Admin Gebruikersnaam: ");
        string username = Console.ReadLine();

        Console.Write("Admin Wachtwoord: ");
        string password = Console.ReadLine();

        if (username == "admin" && password == "admin123")
        {
            Console.WriteLine("Welkom Admin!");
            AdminPanel();
        }
        else
        {
            Console.WriteLine("Ongeldige gebruikersnaam of wachtwoord.");
            Console.ReadKey();
        }
    }

    public static void AdminPanel()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("1. Manipuleer weddenschapresultaten");
            Console.WriteLine("2. Log uit als admin");
            Console.WriteLine("3. Haal wedstrijden op via API");
            Console.Write("Kies een optie: ");
            string keuze = Console.ReadLine();

            switch (keuze)
            {
                case "1":
                    ManipuleerWeddenschappen();
                    break;
                case "2":
                    Console.WriteLine("Je bent uitgelogd als admin.");
                    return;
                case "3":
                    FetchMatchesFromAPI();
                    break;
                default:
                    Console.WriteLine("Ongeldige keuze.");
                    break;
            }
        }
    }

    public static void ManipuleerWeddenschappen()
    {
        var wedstrijden = GetAvailableMatches();
        if (wedstrijden.Count == 0)
        {
            Console.WriteLine("Er zijn geen beschikbare wedstrijden om te manipuleren.");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Beschikbare wedstrijden om te manipuleren:");
        for (int i = 0; i < wedstrijden.Count; i++)
        {
            Console.WriteLine($"{i + 1}: {wedstrijden[i].TeamA} vs {wedstrijden[i].TeamB}");
        }

        Console.Write("Kies het nummer van de wedstrijd die je wilt manipuleren: ");
        int matchChoice;
        if (!int.TryParse(Console.ReadLine(), out matchChoice) || matchChoice < 1 || matchChoice > wedstrijden.Count)
        {
            Console.WriteLine("Ongeldige keuze.");
            Console.ReadKey();
            return;
        }

        var selectedMatch = wedstrijden[matchChoice - 1];
        Console.WriteLine($"Je hebt gekozen voor: {selectedMatch.TeamA} vs {selectedMatch.TeamB}");
        Console.Write("Kies de winnaar (1 voor team A, 2 voor team B): ");
        int winnerChoice;
        if (!int.TryParse(Console.ReadLine(), out winnerChoice) || winnerChoice < 1 || winnerChoice > 2)
        {
            Console.WriteLine("Ongeldige keuze.");
            Console.ReadKey();
            return;
        }

        string winner = winnerChoice == 1 ? selectedMatch.TeamA : selectedMatch.TeamB;
        UpdateMatchResult(selectedMatch.Id, winner);
        Console.WriteLine($"Wedstrijdresultaat bijgewerkt naar: {winner} wint.");
        Console.ReadKey();
    }
}