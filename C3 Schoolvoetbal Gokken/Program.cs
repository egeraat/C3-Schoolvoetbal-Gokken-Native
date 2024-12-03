using MySql.Data.MySqlClient;
using System;

class Program
{
    static readonly string connectionString = "Server=localhost;Database=bets_db;Uid=c_sharp_dev;Pwd=c_sharp_dev;";

    static void Main()
    {
        InitializeDatabase();

        while (true)
        {
            Console.WriteLine("1. Maak een account");
            Console.WriteLine("2. Log in");
            Console.WriteLine("3. Sluit af");
            Console.Write("Kies een optie: ");
            string keuze = Console.ReadLine();

            switch (keuze)
            {
                case "1":
                    MaakAccount();
                    break;
                case "2":
                    LogIn();
                    break;
                case "3":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Ongeldige keuze.");
                    break;
            }
        }
    }

    static void InitializeDatabase()
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            string createUsersTable = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(50) NOT NULL UNIQUE,
                    Password VARCHAR(50) NOT NULL,
                    Balance DECIMAL(10, 2) DEFAULT 50.00
                );";

            string createMatchesTable = @"
                CREATE TABLE IF NOT EXISTS Matches (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    TeamA VARCHAR(50),
                    TeamB VARCHAR(50),
                    Result VARCHAR(50)
                );";

            string createBetsTable = @"
                CREATE TABLE IF NOT EXISTS Bets (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT,
                    BetDescription VARCHAR(255),
                    BetAmount DECIMAL(10, 2),
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );";

            using (var command = new MySqlCommand(createUsersTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new MySqlCommand(createMatchesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new MySqlCommand(createBetsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            string checkBalanceColumn = "SHOW COLUMNS FROM Users LIKE 'Balance';";
            using (var command = new MySqlCommand(checkBalanceColumn, connection))
            {
                var result = command.ExecuteScalar();
                if (result == null)
                {
                    string addBalanceColumn = "ALTER TABLE Users ADD COLUMN Balance DECIMAL(10, 2) DEFAULT 50.00;";
                    using (var alterCommand = new MySqlCommand(addBalanceColumn, connection))
                    {
                        alterCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }

    static void MaakAccount()
    {
        Console.Write("Voer een gebruikersnaam in: ");
        string username = Console.ReadLine();
        Console.Write("Voer een wachtwoord in: ");
        string password = Console.ReadLine();

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "INSERT INTO Users (Username, Password) VALUES (@username, @password);";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);

                try
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Account succesvol aangemaakt! Je start met 50 4S-dollars.");
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"Fout bij het aanmaken van account: {ex.Message}");
                }
            }
        }
    }
    //
    static void LogIn()
    {
        Console.Write("Gebruikersnaam: ");
        string username = Console.ReadLine();
        Console.Write("Wachtwoord: ");
        string password = Console.ReadLine();

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Id, Balance FROM Users WHERE Username = @username AND Password = @password;";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int userId = reader.GetInt32("Id");
                        decimal balance = reader.GetDecimal("Balance");
                        Console.WriteLine($"Succesvol ingelogd! Uw saldo is: 4S-{balance}");
                        ManageBets(userId);
                    }
                    else
                    {
                        Console.WriteLine("Ongeldige inloggegevens.");
                    }
                }
            }
        }
    }

    static void ManageBets(int userId)
    {
        while (true)
        {
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
                    ToonSaldo(userId);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Ongeldige keuze.");
                    break;
            }
        }
    }

    static void ToonSaldo(int userId)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Balance FROM Users WHERE Id = @userId;";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);

                object result = command.ExecuteScalar();
                if (result != null)
                {
                    decimal saldo = Convert.ToDecimal(result);
                    Console.WriteLine($"Je huidige saldo is: €{saldo:F2}");
                }
                else
                {
                    Console.WriteLine("Saldo kon niet worden opgehaald.");
                }
            }
        }
    }

    static void PlaatsWeddenschap(int userId)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Id, TeamA, TeamB FROM Matches WHERE Result IS NULL;";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                Console.WriteLine("Beschikbare wedstrijden:");
                while (reader.Read())
                {
                    int matchId = reader.GetInt32("Id");
                    string teamA = reader.GetString("TeamA");
                    string teamB = reader.GetString("TeamB");
                    Console.WriteLine($"{matchId}: {teamA} vs {teamB}");
                }
            }

            Console.Write("Voer het ID van de wedstrijd in: ");
            if (int.TryParse(Console.ReadLine(), out int gekozenMatchId))
            {
                if (!WedstrijdBestaat(gekozenMatchId, connection))
                {
                    Console.WriteLine("De opgegeven wedstrijd ID bestaat niet.");
                    return;
                }

                Console.Write("Op welk team wil je wedden? (TeamA/TeamB): ");
                string betOnTeam = Console.ReadLine();
                Console.Write("Inzet bedrag in 4S-dollars: ");
                if (decimal.TryParse(Console.ReadLine(), out decimal betAmount))
                {
                    string insertQuery = "INSERT INTO Bets (UserId, MatchId, BetOnTeam, BetAmount) VALUES (@userId, @matchId, @betOnTeam, @betAmount);";

                    using (var insertCommand = new MySqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@userId", userId);
                        insertCommand.Parameters.AddWithValue("@matchId", gekozenMatchId);
                        insertCommand.Parameters.AddWithValue("@betOnTeam", betOnTeam);
                        insertCommand.Parameters.AddWithValue("@betAmount", betAmount);

                        insertCommand.ExecuteNonQuery();
                        Console.WriteLine("Weddenschap succesvol geplaatst!");
                    }
                }
                else
                {
                    Console.WriteLine("Ongeldig bedrag.");
                }
            }
            else
            {
                Console.WriteLine("Ongeldig wedstrijd ID.");
            }
        }
    }

    static bool WedstrijdBestaat(int wedstrijdId, MySqlConnection connection)
    {
        string query = "SELECT COUNT(*) FROM Matches WHERE Id = @wedstrijdId;";
        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@wedstrijdId", wedstrijdId);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
    }

    static void BekijkWeddenschappen(int userId)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Bets.Id, Matches.TeamA, Matches.TeamB, Bets.BetOnTeam, Bets.BetAmount, Bets.HasWon " +
                           "FROM Bets JOIN Matches ON Bets.MatchId = Matches.Id WHERE Bets.UserId = @userId;";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);

                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("Uw weddenschappen:");
                    while (reader.Read())
                    {
                        int betId = reader.GetInt32("Id");
                        string teamA = reader.GetString("TeamA");
                        string teamB = reader.GetString("TeamB");
                        string betOnTeam = reader.GetString("BetOnTeam");
                        decimal betAmount = reader.GetDecimal("BetAmount");
                        string hasWon = reader.IsDBNull(reader.GetOrdinal("HasWon")) ? "In behandeling" : reader.GetBoolean("HasWon") ? "Gewonnen" : "Verloren";

                        Console.WriteLine($"{betId}: {teamA} vs {teamB} - Wed op {betOnTeam}, Bedrag: {betAmount} - {hasWon}");
                    }
                }
            }
        }
    }
}
