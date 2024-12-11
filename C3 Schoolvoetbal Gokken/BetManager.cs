using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

public static class BetManager
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
                default:
                    Console.WriteLine("Ongeldige keuze.");
                    break;
            }
        }
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
        Console.Write("Op welk team wil je wedden?");
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

            // Start a transaction to ensure consistency
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // Insert the bet into the Bets table with Amount included
                    string insertBetQuery = @"
                        INSERT INTO Bets (UserId, MatchId, BetOnTeam, BetAmount, TeamA, TeamB) 
                        VALUES (@userId, @matchId, @betOnTeam, @betAmount, @teamA, @teamB);";
                    using (var command = new MySqlCommand(insertBetQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@matchId", matchId);
                        command.Parameters.AddWithValue("@betOnTeam", betOnTeam);
                        command.Parameters.AddWithValue("@betAmount", betAmount);  // Ensure Amount is passed correctly
                        command.Parameters.AddWithValue("@teamA", teamA);
                        command.Parameters.AddWithValue("@teamB", teamB);
                        command.ExecuteNonQuery();
                    }

                    // Update the user's balance after the bet
                    string updateBalanceQuery = "UPDATE Users SET Balance = Balance - @betAmount WHERE Id = @userId;";
                    using (var updateCommand = new MySqlCommand(updateBalanceQuery, connection, transaction))
                    {
                        updateCommand.Parameters.AddWithValue("@betAmount", betAmount);
                        updateCommand.Parameters.AddWithValue("@userId", userId);
                        updateCommand.ExecuteNonQuery();
                    }

                    // Commit the transaction
                    transaction.Commit();
                }
                catch (Exception)
                {
                    // Rollback the transaction if something goes wrong
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public static void BekijkWeddenschappen(int userId)
    {
        Console.Clear();
        var bets = GetUserBets(userId);

        if (bets.Count == 0)
        {
            Console.WriteLine("Je hebt geen weddenschappen geplaatst.");
        }
        else
        {
            Console.WriteLine("Uw weddenschappen:");
            foreach (var bet in bets)
            {
                Console.WriteLine($"Wedstrijd {bet.TeamA} vs {bet.TeamB}, Ingezet op: {bet.BetOnTeam}, Inzet: {bet.BetAmount} 4S-dollars");
            }
        }
        Console.ReadKey();
    }

    public static List<Bet> GetUserBets(int userId)
    {
        var bets = new List<Bet>();
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Matches.TeamA, Matches.TeamB, Bets.BetOnTeam, Bets.BetAmount " +
                           "FROM Bets JOIN Matches ON Bets.MatchId = Matches.Id WHERE Bets.UserId = @userId;";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var bet = new Bet
                        {
                            TeamA = reader.GetString("TeamA"),
                            TeamB = reader.GetString("TeamB"),
                            BetOnTeam = reader.GetString("BetOnTeam"),
                            BetAmount = reader.GetDecimal("BetAmount")
                        };
                        bets.Add(bet);
                    }
                }
            }
        }
        return bets;
    }

    public static List<Match> GetAvailableMatches()
    {
        var matches = new List<Match>();
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Id, TeamA, TeamB FROM Matches WHERE Result IS NULL;";
            using (var command = new MySqlCommand(query, connection))
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
        return matches;
    }
}
