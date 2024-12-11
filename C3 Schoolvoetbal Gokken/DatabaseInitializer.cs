using MySql.Data.MySqlClient;

public static class DatabaseInitializer
{
    static readonly string connectionString = "Server=localhost;Database=bets_db;Uid=c_sharp_dev;Pwd=c_sharp_dev;";

    public static void InitializeDatabase()
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
                    TeamA VARCHAR(50) NOT NULL,
                    TeamB VARCHAR(50) NOT NULL,
                    Result VARCHAR(50)
                );";

            string createBetsTable = @"
                CREATE TABLE IF NOT EXISTS Bets (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT,
                    MatchId INT,
                    BetOnTeam VARCHAR(50),
                    BetAmount DECIMAL(10, 2),
                    FOREIGN KEY (UserId) REFERENCES Users(Id),
                    FOREIGN KEY (MatchId) REFERENCES Matches(Id)
                );";

            ExecuteNonQuery(createUsersTable, connection);
            ExecuteNonQuery(createMatchesTable, connection);
            ExecuteNonQuery(createBetsTable, connection);
        }
    }

    private static void ExecuteNonQuery(string query, MySqlConnection connection)
    {
        using (var command = new MySqlCommand(query, connection))
        {
            command.ExecuteNonQuery();
        }
    }
}
