using System;

class Program
{
    static void Main()
    {
        DatabaseInitializer.InitializeDatabase();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("1. Maak een account");
            Console.WriteLine("2. Log in");
            Console.WriteLine("3. Sluit af");
            Console.Write("Kies een optie: ");
            string keuze = Console.ReadLine();

            switch (keuze)
            {
                case "1":
                    AccountManager.MaakAccount();
                    break;
                case "2":
                    AccountManager.LogIn();
                    break;
                case "3":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Ongeldige keuze. Probeer het opnieuw.");
                    break;
            }
        }
    }
}
