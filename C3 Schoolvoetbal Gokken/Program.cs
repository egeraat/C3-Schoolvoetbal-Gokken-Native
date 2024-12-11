using System;

public class Program
{
    public static void Main()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Maak een keuze");
            Console.WriteLine("1. Maak een account");
            Console.WriteLine("2. Login");
            Console.WriteLine("3. Exit");
            Console.Write("Kies een optie: ");
            string keuze = Console.ReadLine();

            switch (keuze)
            {
                case "1":
                    AccountManager.MaakAccount();
                    break;
                case "2":
                    int userId = AccountManager.Login();
                    if (userId != -1)
                    {
                        BetManager.ManageBets(userId);
                    }
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Ongeldige keuze.");
                    break;
            }
        }
    }
}
