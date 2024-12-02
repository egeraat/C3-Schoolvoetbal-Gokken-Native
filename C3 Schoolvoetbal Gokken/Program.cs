global using MySql.Data.MySqlClient;
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

            string createUsersTable = "CREATE TABLE IF NOT EXISTS Users (" +
                                      "Id INT AUTO_INCREMENT PRIMARY KEY, " +
                                      "Username VARCHAR(50) NOT NULL UNIQUE, " +
                                      "Password VARCHAR(50) NOT NULL);";

            string createBetsTable = "CREATE TABLE IF NOT EXISTS Bets (" +
                                     "Id INT AUTO_INCREMENT PRIMARY KEY, " +
                                     "UserId INT, " +
                                     "BetDescription VARCHAR(255), " +
                                     "BetAmount DECIMAL(10, 2), " +
                                     "FOREIGN KEY (UserId) REFERENCES Users(Id));";

            using (var command = new MySqlCommand(createUsersTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new MySqlCommand(createBetsTable, connection))
            {
                command.ExecuteNonQuery();
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
                    Console.WriteLine("Account succesvol aangemaakt!");
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"Fout bij het aanmaken van account: {ex.Message}");
                }
            }
        }
    }

    static void LogIn()
    {
        Console.Write("Gebruikersnaam: ");
        string username = Console.ReadLine();
        Console.Write("Wachtwoord: ");
        string password = Console.ReadLine();

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT Id FROM Users WHERE Username = @username AND Password = @password;";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int userId = reader.GetInt32("Id");
                        Console.WriteLine("Succesvol ingelogd!");
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
            Console.WriteLine("1. Voeg een weddenschap toe");
            Console.WriteLine("2. Bekijk weddenschappen");
            Console.WriteLine("3. Log uit");
            Console.Write("Kies een optie: ");
            string keuze = Console.ReadLine();

            switch (keuze)
            {
                case "1":
                    VoegWeddenschapToe(userId);
                    break;
                case "2":
                    BekijkWeddenschappen(userId);
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Ongeldige keuze.");
                    break;
            }
        }
    }

    static void VoegWeddenschapToe(int userId)
    {
        Console.Write("Beschrijving van de weddenschap: ");
        string beschrijving = Console.ReadLine();
        Console.Write("Inzet bedrag: ");
        if (decimal.TryParse(Console.ReadLine(), out decimal bedrag))
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Bets (UserId, BetDescription, BetAmount) VALUES (@userId, @beschrijving, @bedrag);";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@beschrijving", beschrijving);
                    command.Parameters.AddWithValue("@bedrag", bedrag);

                    command.ExecuteNonQuery();
                    Console.WriteLine("Weddenschap succesvol toegevoegd!");
                }
            }
        }
        else
        {
            Console.WriteLine("Ongeldig bedrag.");
        }
    }

    static void BekijkWeddenschappen(int userId)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT BetDescription, BetAmount FROM Bets WHERE UserId = @userId;";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);

                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("Uw weddenschappen:");
                    while (reader.Read())
                    {
                        string beschrijving = reader.GetString("BetDescription");
                        decimal bedrag = reader.GetDecimal("BetAmount");
                        Console.WriteLine($"- {beschrijving}: €{bedrag}");
                    }
                }
            }
        }
    }
}
