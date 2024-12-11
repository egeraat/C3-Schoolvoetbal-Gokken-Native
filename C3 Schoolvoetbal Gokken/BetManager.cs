using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

public class BetManager
{
    static readonly string connectionString = "Server=localhost;Database=bets_db;Uid=c_sharp_dev;Pwd=c_sharp_dev;";

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

    public static void BekijkWeddenschappen(int userId)
    {
        Console.Clear();
        Console.WriteLine("Je geplaatste weddenschappen:");

        var placedBets = GetUserBets(userId);
        if (placedBets.Count == 0)
        {
            Console.WriteLine("Je hebt geen weddenschappen geplaatst.");
            Console.ReadKey();
            return;
        }

        foreach (var bet in placedBets)
        {
            Console.WriteLine($"Wedstrijd: {bet.TeamA} vs {bet.TeamB} - Je wedde op: {bet.BetOnTeam} - Inzet: {bet.BetAmount} 4S-dollars");
        }

        Console.ReadKey();
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

        var geselecteerdeWedstrijd = wedstrijden[matchChoice - 1];
        Console.WriteLine($"Je hebt gekozen voor: {geselecteerdeWedstrijd.TeamA} vs {geselecteerdeWedstrijd.TeamB}");
        Console.Write("Voer de nieuwe uitkomst in (TeamA of TeamB): ");
        string result = Console.ReadLine().Trim();

        if (result != geselecteerdeWedstrijd.TeamA && result != geselecteerdeWedstrijd.TeamB)
        {
            Console.WriteLine("Ongeldige keuze. De uitkomst moet overeenkomen met een van de teams.");
            Console.ReadKey();
            return;
        }

        ManipuleerWedstrijdResultaat(geselecteerdeWedstrijd.Id, result);
        Console.WriteLine($"Wedstrijdresultaat succesvol gemanipuleerd!");
        VerwerkWeddenschappenNaManipulatie(geselecteerdeWedstrijd.Id, result);
        Console.ReadKey();
    }

    public static void ManipuleerWedstrijdResultaat(int matchId, string result)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "UPDATE Matches SET Result = @result WHERE Id = @matchId;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@matchId", matchId);
                command.Parameters.AddWithValue("@result", result);
                command.ExecuteNonQuery();
            }
        }
    }

    public static void VerwerkWeddenschappenNaManipulatie(int matchId, string winningTeam)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = @"
                SELECT Bets.UserId, Bets.BetAmount, Bets.BetOnTeam
                FROM Bets 
                JOIN Matches ON Bets.MatchId = Matches.Id 
                WHERE Bets.MatchId = @matchId;";

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

                        decimal newBalance = GetUserBalance(userId);
                        if (betOnTeam.ToUpper() == winningTeam.ToUpper())
                        {
                            newBalance += betAmount * 2;
                        }

                        string updateQuery = "UPDATE Users SET Balance = @newBalance WHERE Id = @userId;";
                        using (var updateCommand = new MySqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@newBalance", newBalance);
                            updateCommand.Parameters.AddWithValue("@userId", userId);
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}
