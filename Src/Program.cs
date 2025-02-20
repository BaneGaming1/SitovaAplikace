using DotNetEnv;
using System;

namespace SíťovýProjekt.Src
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                string configPath = "config.json";

                // 1) Načíst JSON s konfigurací
                ConfigManager config = ConfigManager.LoadConfig(configPath);

                // 2) Vytvořit bankovní uzel (node) a spustit ho
                BankNode node = new BankNode(config);
                node.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chyba při spuštění aplikace: " + ex.Message);
            }
        }
    }
}
