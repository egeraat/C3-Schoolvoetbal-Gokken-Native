using MySql.Data.MySqlClient;
using System;

public static class AccountManager
{
    static readonly string connectionString = "Server=localhost;Database=bets_db;Uid=c_sharp_dev;Pwd=c_sharp_dev;";

    public static void MaakAccount()
    {
        Console.Clear();
        Console.Write("Voer een gebruikersnaam in: ");
        string username = Console.ReadLine();
        Console.Write("Voer een wachtwoord in: ");
        string password = Console.ReadLine();

        if (AccountBestaat(username))
        {
            Console.WriteLine("Account bestaat al! Kies een andere gebruikersnaam.");
            return;
        }

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "INSERT INTO Users (Username, Password, Balance) VALUES (@username, @password, 50.00);";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);
                command.ExecuteNonQuery();
                Console.WriteLine("Account succesvol aangemaakt! Je begint met 50 4S-dollars.");
            }
        }
    }

    private static bool AccountBestaat(string username)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT COUNT(*) FROM Users WHERE Username = @username;";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }
    }

    public static void LogIn()
    {
        Console.Clear();
        Console.Write("Gebruikersnaam: ");
        string username = Console.ReadLine();
        Console.Write("Wachtwoord: ");
        string password = Console.ReadLine();

        int userId = ValidUserLogin(username, password);
        if (userId == -1)
        {
            Console.WriteLine("Ongeldige inloggegevens.");
            return;
        }

        Console.WriteLine($"Succesvol ingelogd! Uw saldo is: €{BetManager.GetUserBalance(userId):F2}");
        BetManager.ManageBets(userId);
    }

    private static int ValidUserLogin(string username, string password)
    {
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
                        return reader.GetInt32("Id");
                }
            }
        }
        return -1;
    }
}
