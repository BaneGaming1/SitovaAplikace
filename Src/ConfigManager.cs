using System;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SíťovýProjekt.Src
{
    /// <summary>
    /// Třída ConfigManager slouží ke čtení a validaci konfiguračních nastavení aplikace.
    /// Konfigurace se načítá ze souboru ve formátu JSON a obsahuje nastavení jako IP adresa, port,
    /// timeout a cesta k souboru pro logování.
    /// </summary>
    public class ConfigManager
    {
        // Vlastnosti, které představují jednotlivá nastavení načtená z konfiguračního souboru

        /// <summary>
        /// IP adresa, na které aplikace naslouchá.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Port, na kterém aplikace naslouchá.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Timeout (v sekundách) pro operace, např. síťová komunikace.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Cesta k souboru, do kterého se zapisují logy aplikace.
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// Statická metoda pro načtení konfiguračního souboru z dané cesty a deserializaci obsahu JSON do instance ConfigManager.
        /// Následně provede validaci načtené konfigurace.
        /// </summary>
        /// <param name="path">Cesta k souboru s konfigurací.</param>
        /// <returns>Instance ConfigManager s načtenými hodnotami.</returns>
        public static ConfigManager LoadConfig(string path)
        {
            // Načte celý obsah souboru jako řetězec.
            string json = File.ReadAllText(path);
            // Deserializuje JSON řetězec do instance ConfigManager.
            var config = JsonSerializer.Deserialize<ConfigManager>(json);
            Validate(config);
            return config;
        }

        /// <summary>
        /// Metoda, která provádí základní validaci načtené konfigurace.
        /// Zkontroluje platnost IP adresy, rozsah portu, hodnotu timeoutu a že cesta k logovacímu souboru není prázdná.
        /// Pokud některá z podmínek není splněna, vyhodí výjimku s příslušnou zprávou.
        /// </summary>
        /// <param name="config">Konfigurace, která má být validována.</param>
        private static void Validate(ConfigManager config)
        {
            // Kontrola, zda je zadaná IP adresa ve správném formátu (čtyři skupiny čísel oddělené tečkami) 
            // a zda jde o platnou IP adresu.
            if (!Regex.IsMatch(config.IpAddress, @"^(\d{1,3}\.){3}\d{1,3}$") // kontroloa pokud není ip ve správném formátu 
                || !IPAddress.TryParse(config.IpAddress, out _))             // || (nebo) pokud ip není platná podle IPAddress.TryParse
            {
                throw new ArgumentException("Neplatná IP adresa v konfiguraci.");
            }

            // Ověření, že port je v povoleném rozsahu (65525 až 65535).
            if (config.Port < 65525 || config.Port > 65535)
            {
                throw new ArgumentException("Port musí být v rozsahu 65525-65535.");
            }

            // Kontrola, že hodnota timeoutu je kladné číslo.
            if (config.Timeout <= 0)
            {
                throw new ArgumentException("Timeout musí být kladné číslo.");
            }

            // Ověření, že cesta k logovacímu souboru není prázdná.
            if (string.IsNullOrEmpty(config.LogFile))
            {
                throw new ArgumentException("Cesta k logovacímu souboru nemůže být prázdná.");
            }
        }
    }
}
