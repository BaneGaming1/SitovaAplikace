using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SíťovýProjekt.Src
{
    /// <summary>
    /// Třída <c>BankNode</c> představuje bankovní uzel, který zpracovává příchozí síťové požadavky,
    /// komunikuje s databází a případně forwarduje požadavky na jiné banky.
    /// </summary>
    public class BankNode
    {
        // Instance třídy pro načítání konfigurace aplikace (obsahuje např. IP adresu, port, timeout a cestu k logovacímu souboru)
        private ConfigManager config;

        // Instance třídy pro logování zpráv (zapisuje logy do konzole a do souboru)
        private Logger logger;

        // Instance TcpListener, který naslouchá na zadaném portu a přijímá síťová spojení
        private TcpListener server;

        /// <summary>
        /// Konstruktor třídy <c>BankNode</c>.
        /// Inicializuje konfiguraci a logger.
        /// </summary>
        /// <param name="config">Objekt konfigurace, který obsahuje nastavení uzlu.</param>
        public BankNode(ConfigManager config)
        {
            this.config = config;
            logger = new Logger(config.LogFile);
        }

        /// <summary>
        /// Spustí bankovní uzel.
        /// Metoda inicializuje TcpListener, který naslouchá na zadané IP adrese a portu,
        /// a poté čeká na příchozí spojení. Každé spojení se obsluhuje asynchronně.
        /// </summary>
        public void Start()
        {
            // Vytvoření instance TcpListener podle IP adresy a portu z konfigurace
            server = new TcpListener(IPAddress.Parse(config.IpAddress), config.Port); //.Parse převádí string na objekt IPAddress pro TcpListener.
            server.Start();
            logger.Info($"Bank node started on {config.IpAddress}:{config.Port}");

            // Nekonečná smyčka pro přijímání příchozích klientských spojení
            while (true)
            {
                TcpClient client = server.AcceptTcpClient(); //// Čeká na připojení klienta

                // Každého klienta obsloužíme asynchronně v novém vlákně (Task.Run)
                Task.Run(() => HandleClient(client));
            }
        }

        /// <summary>
        /// Obsluhuje příchozí klientské spojení.
        /// Nastavuje timeouty pro příjem i odesílání dat a zpracovává příchozí požadavky.
        /// </summary>
        /// <param name="client">Instance <c>TcpClient</c> reprezentující spojení s klientem.</param>
        private void HandleClient(TcpClient client)
        {
            // Nastavení timeoutů: 60 sekund pro příjem a podle konfigurace pro odeslání (milisekundy * 1000)
            client.ReceiveTimeout = 60 * 1000;
            client.SendTimeout = config.Timeout * 1000; 

            // Používáme using blok pro správné uvolnění prostředků spojených se síťovým streamem
            using (NetworkStream stream = client.GetStream()) //datový proud (stream), který umožňuje čtení a zápis dat mezi serverem a klientem.
            using (var reader = new StreamReader(stream, Encoding.UTF8)) //čte data od klienta
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true }) // posílá odpovědi klientovi AutoFlush = true → Každá zpráva se ihned odešle (nemusíme volat Flush() manuálně).
            {
                try
                {
                    // Smyčka pro čtení požadavků od klienta
                    while (true)
                    {
                        // Čtení řádku textu z klientského streamu (string?- může být null. např klient ukončí spojení)
                        string? request = reader.ReadLine();
                        if (request == null) break; // Konec spojení, pokud není žádný požadavek

                        // Filtrace příchozího řetězce – odstranění nežádoucích znaků,
                        // ponecháme pouze alfanumerické znaky, mezery, lomítka a tečky (předcházíme utokum např sql injection)
                        request = new string(request
                            .Where(c => char.IsLetterOrDigit(c)
                                     || char.IsWhiteSpace(c)
                                     || c == '/' || c == '.')
                            .ToArray()).Trim(); // .ToArray() převádí kolekci znaků na pole znaků pro vytvoření řetězce. (potřebujeme string)

                        // Zpracování požadavku a získání odpovědi
                        string response = ProcessCommand(request);

                        // Odeslání odpovědi klientovi
                        writer.WriteLine(response);
                    }
                }
                catch (Exception e)
                {
                    // V případě chyby se zapíše chybová zpráva do logu
                    logger.Error("Error: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Hlavní router příkazů.
        /// Na základě prvního tokenu (příkazu) volá odpovídající metodu pro zpracování.
        /// </summary>
        /// <param name="request">Textový řetězec obsahující příkaz a parametry.</param>
        /// <returns>Odpověď, kterou se má klientovi vrátit.</returns>
        private string ProcessCommand(string request)
        {
            // Zalogování přijatého příkazu
            logger.Info($"Příchozí příkaz: '{request}'");

            // Kontrola, zda řetězec není prázdný
            if (string.IsNullOrWhiteSpace(request))
                return "ER Neplatný příkaz (prázdný).";

            // Rozdělení požadavku na části pomocí mezery jako oddělovače
            string[] parts = request.Split(' ', StringSplitOptions.RemoveEmptyEntries); //StringSplitOptions.RemoveEmptyEntries odstraní prázdné části, pokud je v textu více mezer za sebou.

            // První část představuje samotný příkaz např. AC
            string command = parts[0];

            // Přepínač, který volá odpovídající metodu na základě příkazu
            switch (command)
            {
                case "BC":
                    return BankCode(parts);

                case "AC":
                    return AccountCreate(parts);

                case "AD":
                    return AccountDeposit(parts);

                case "AW":
                    return AccountWithdrawal(parts);

                case "AB":
                    return AccountBalance(parts);

                case "AR":
                    return AccountRemove(parts);

                case "BA":
                    return BankTotalAmount(parts);

                case "BN":
                    return BankNumberClients(parts);

                default:
                    return "ER Neznámý příkaz.";
            }
        }

        /// <summary>
        /// Vrací kód (IP) banky.
        /// Příklad: Pokud je příkaz "BC", vrátí se "BC 10.1.2.3" (IP adresa z konfigurace).
        /// </summary>
        /// <param name="parts">Pole řetězců z příkazu.</param>
        /// <returns>Odpověď s IP adresou banky.</returns>
        public string BankCode(string[] parts)
        {
            // Pokud příkaz obsahuje pouze "BC" bez dalších parametrů
            if (parts.Length == 1)
            {
                return $"BC {config.IpAddress}";
            }
            else
            {
                return "ER Neplatný formát příkazu BC.";
            }
        }

        /// <summary>
        /// Vytváří nový bankovní účet.
        /// Generuje náhodné číslo účtu v rozsahu 10000 až 99999 a ukládá ho do databáze.
        /// Příklad: AC -> "AC 10001/10.1.2.3"
        /// </summary>
        /// <param name="parts">Pole řetězců z příkazu.</param>
        /// <returns>Odpověď s číslem nově vytvořeného účtu a IP adresou banky.</returns>
        public string AccountCreate(string[] parts)
        {
            // Pokud je příkaz AC zadán s parametry, vrátí se chybová hláška
            if (parts.Length != 1)
                return "ER Neplatný formát příkazu AC. Očekávám 'AC' bez parametrů.";

            Random rnd = new Random();
            int accountNumber;
            int pokusy = 0; // zabraňuje nekonečné smyčce, pokud je většina čísel už obsazena

            // Použití databázového kontextu pro vytvoření nového účtu
            using (var context = new BankContext())
            {
                // Generování unikátního čísla účtu
                do
                {
                    accountNumber = rnd.Next(10000, 100000);
                    pokusy++;
                    if (pokusy > 100000) //Bez omezení by mohl cyklus běžet nekonečně dlouho, pokud by nebylo žádné dostupné číslo.
                        return "ER Nepodařilo se vytvořit nový účet, zkuste to znovu.";
                }
                while (context.BankAccounts.Any(a => a.AccountID == accountNumber)); //// Opakuj, dokud číslo účtu existuje v databázi (hledáme unikátní)

                // Vytvoření nové instance účtu s vygenerovaným číslem a počátečním zůstatkem 0
                var newAccount = new BankAccount
                {
                    AccountID = accountNumber,
                    Balance = 0
                };

                // Přidání nového účtu do databáze a uložení změn
                context.BankAccounts.Add(newAccount);
                context.SaveChanges();
            }

            return $"AC {accountNumber}/{config.IpAddress}";
        }

        /// <summary>
        /// Provádí vklad peněz na účet.
        /// Příkaz má formát: "AD <account>/<ip> <amount>"
        /// </summary>
        /// <param name="parts">Pole řetězců z příkazu.</param>
        /// <returns>"AD" v případě úspěchu nebo chybovou zprávu.</returns>
        public string AccountDeposit(string[] parts)
        {
            // Kontrola správného formátu příkazu
            if (parts.Length < 3)
                return "ER Neplatný formát příkazu. Očekávám 'AD <account>/<ip> <amount>'.";

            // Pokus o parsování čísla účtu a IP adresy z prvního parametru
            if (!TryParseAccountAndIp(parts[1], out int accountId, out string accountIp, out string errorMsg))
            {
                return $"ER {errorMsg}";
            }

            // Pokus o parsování částky a kontrola, zda je nezáporná
            if (!long.TryParse(parts[2], out long amount) || amount < 0) //out long amount- TryParse se pokusí převést text na číslo a uloží výsledek do amount
            {
                return "ER Špatný formát čísla částky (musí být nezáporné celé číslo).";
            }

            // Pokud účet nepatří do této banky, forwarduje se požadavek na cílovou banku
            if (accountIp != config.IpAddress)
            {
                logger.Info($"Forwarduji příkaz AD do banky {accountIp}...");
                return SendToServer(string.Join(' ', parts), accountIp); //string.Join(' ', parts) převádí pole parts[] zpět na řetězec, aby se příkaz poslal v původní podobě.
            }

            // Použití databázového kontextu pro aktualizaci zůstatku účtu
            using (var context = new BankContext())
            {
                var account = context.BankAccounts.Find(accountId);
                if (account == null)
                    return "ER Účet neexistuje (v této bance).";

                // Zvýšení zůstatku o zadanou částku
                account.Balance += amount;
                context.SaveChanges();
            }

            return "AD";
        }

        /// <summary>
        /// Provádí výběr peněz z účtu.
        /// Příkaz má formát: "AW <account>/<ip> <amount>"
        /// </summary>
        /// <param name="parts">Pole řetězců z příkazu.</param>
        /// <returns>"AW" v případě úspěchu nebo chybovou zprávu.</returns>
        public string AccountWithdrawal(string[] parts)
        {
            // Kontrola správného formátu příkazu
            if (parts.Length < 3)
                return "ER Neplatný formát příkazu. Očekávám 'AW <account>/<ip> <amount>'.";

            // Pokus o parsování čísla účtu a IP adresy
            if (!TryParseAccountAndIp(parts[1], out int accountId, out string accountIp, out string errorMsg))
            {
                return $"ER {errorMsg}";
            }

            // Pokus o parsování částky a kontrola, zda je nezáporná
            if (!long.TryParse(parts[2], out long amount) || amount < 0)
            {
                return "ER Špatný formát čísla částky (musí být nezáporné celé číslo).";
            }

            // Forward požadavku, pokud účet nepatří do této banky
            if (accountIp != config.IpAddress)
            {
                logger.Info($"Forwarduji příkaz AW do banky {accountIp}...");
                return SendToServer(string.Join(' ', parts), accountIp);
            }

            // Použití databázového kontextu pro výběr peněz z účtu
            using (var context = new BankContext())
            {
                var account = context.BankAccounts.Find(accountId);
                if (account == null)
                    return "ER Účet neexistuje (v této bance).";

                // Kontrola dostatečného zůstatku pro výběr
                if (account.Balance < amount)
                    return "ER Není dostatek finančních prostředků.";

                // Snížení zůstatku o zadanou částku
                account.Balance -= amount;
                context.SaveChanges();
            }

            return "AW";
        }

        /// <summary>
        /// Vrací zůstatek na účtu.
        /// Příkaz má formát: "AB <account>/<ip>"
        /// </summary>
        /// <param name="parts">Pole řetězců z příkazu.</param>
        /// <returns>Odpověď obsahující zůstatek nebo chybovou zprávu.</returns>
        public string AccountBalance(string[] parts)
        {
            // Kontrola správného formátu příkazu
            if (parts.Length < 2)
                return "ER Neplatný formát příkazu. Očekávám 'AB <account>/<ip>'.";

            // Pokus o parsování čísla účtu a IP adresy
            if (!TryParseAccountAndIp(parts[1], out int accountId, out string accountIp, out string errorMsg))
            {
                return $"ER {errorMsg}";
            }

            // Forward požadavku, pokud účet nepatří do této banky
            if (accountIp != config.IpAddress)
            {
                logger.Info($"Forwarduji příkaz AB do banky {accountIp}...");
                return SendToServer(string.Join(' ', parts), accountIp);
            }

            long balance; //long je datový typ pro velké hodnoty

            // Použití databázového kontextu pro získání zůstatku účtu
            using (var context = new BankContext())
            {
                var account = context.BankAccounts.Find(accountId);
                if (account == null)
                    return "ER Účet neexistuje (v této bance).";

                balance = account.Balance;
            }

            return $"AB {balance}";
        }

        /// <summary>
        /// Odstraňuje účet, pokud má zůstatek 0.
        /// Příkaz má formát: "AR <account>/<ip>"
        /// </summary>
        /// <param name="parts">Pole řetězců z příkazu.</param>
        /// <returns>"AR" v případě úspěchu nebo chybovou zprávu.</returns>
        public string AccountRemove(string[] parts)
        {
            // Kontrola správného formátu příkazu
            if (parts.Length < 2)
                return "ER Neplatný formát příkazu. Očekávám 'AR <account>/<ip>'.";

            // Pokus o parsování čísla účtu a IP adresy
            if (!TryParseAccountAndIp(parts[1], out int accountId, out string accountIp, out string errorMsg))
            {
                return $"ER {errorMsg}";
            }

            // Forward požadavku, pokud účet nepatří do této banky
            if (accountIp != config.IpAddress)
            {
                logger.Info($"Forwarduji příkaz AR do banky {accountIp}...");
                return SendToServer(string.Join(' ', parts), accountIp);
            }

            // Použití databázového kontextu pro odstranění účtu
            using (var context = new BankContext())
            {
                var account = context.BankAccounts.Find(accountId);
                if (account == null)
                    return "ER Účet neexistuje (v této bance).";

                // Účet lze odstranit pouze, pokud má zůstatek 0
                if (account.Balance != 0)
                    return "ER Nelze smazat bankovní účet na kterém jsou finance.";

                context.BankAccounts.Remove(account);
                context.SaveChanges();
            }

            return "AR";
        }

        /// <summary>
        /// Vrací součet všech peněz ve všech účtech.
        /// Příkaz: "BA"
        /// </summary>
        /// <param name="parts">Pole řetězců z příkazu.</param>
        /// <returns>Odpověď obsahující celkový součet nebo chybovou zprávu.</returns>
        public string BankTotalAmount(string[] parts)
        {
            // Příkaz BA by neměl mít žádné dodatečné parametry
            if (parts.Length != 1)
                return "ER Neplatný formát příkazu BA. Očekávám 'BA' bez parametrů.";

            long total = 0;
            // Použití databázového kontextu pro výpočet celkového součtu zůstatků všech účtů
            using (var context = new BankContext())
            {
                total = context.BankAccounts.Sum(a => a.Balance); //// Sečtení všech zůstatků. a-každý prvek kolekce, a.Balance-hodnota, která se sčítá
            }
            return $"BA {total}";
        }

        /// <summary>
        /// Vrací počet klientů, kteří mají v bance účet.
        /// Příkaz: "BN"
        /// </summary>
        /// <param name="parts">Pole řetězců z příkazu.</param>
        /// <returns>Odpověď obsahující počet účtů nebo chybovou zprávu.</returns>
        public string BankNumberClients(string[] parts)
        {
            // Příkaz BN by neměl mít žádné dodatečné parametry
            if (parts.Length != 1)
                return "ER Neplatný formát příkazu BN. Očekávám 'BN' bez parametrů.";

            int count;
            // Použití databázového kontextu pro získání počtu účtů
            using (var context = new BankContext())
            {
                count = context.BankAccounts.Count();
            }
            return $"BN {count}";
        }

        /// <summary>
        /// Forwarduje (přesměrovává) příkaz do jiné banky.
        /// Naváže spojení na zadanou IP adresu, pošle původní požadavek a přečte odpověď.
        /// </summary>
        /// <param name="request">Původní požadavek jako řetězec.</param>
        /// <param name="targetIp">Cílová IP adresa banky, do které se má požadavek forwardovat.</param>
        /// <returns>Odpověď získanou od cílové banky nebo chybovou zprávu.</returns>
        private string SendToServer(string request, string targetIp)
        {
            try
            {
                // Vytvoření nového klienta pro navázání spojení
                using (TcpClient client = new TcpClient())
                {
                    // Nastavení timeoutů pro příjem a odeslání dat
                    client.ReceiveTimeout = 60 * 1000;
                    client.SendTimeout = config.Timeout * 1000;

                    // Navázání spojení na cílovou banku na stejném portu
                    client.Connect(targetIp, config.Port);

                    // Použití using bloku pro správné uvolnění prostředků spojených se síťovým streamem
                    using (NetworkStream stream = client.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // Odeslání původního požadavku
                        writer.WriteLine(request);

                        // Přečtení odpovědi od cílové banky
                        string? response = reader.ReadLine();

                        // Pokud je odpověď prázdná, vrátí se chybová zpráva
                        if (string.IsNullOrEmpty(response))
                            return "ER Chyba při komunikaci s cílovou bankou (prázdná odpověď).";

                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                // V případě chyby se zapíše chybová zpráva do logu a vrátí se chybová odpověď
                logger.Error($"Forward na {targetIp} selhal: {ex.Message}");
                return "ER Chyba při komunikaci s cílovou bankou.";
            }
        }

        /// <summary>
        /// Pomocná metoda pro parsování řetězce ve formátu "<account>/<ip>".
        /// Například: "10001/10.1.2.3"
        /// </summary>
        /// <param name="input">Řetězec obsahující číslo účtu a IP adresu oddělené lomítkem.</param>
        /// <param name="account">Výstupní parametr, kam se uloží číslo účtu.</param>
        /// <param name="ip">Výstupní parametr, kam se uloží IP adresa.</param>
        /// <param name="errorMsg">Výstupní parametr obsahující chybovou zprávu v případě neúspěchu.</param>
        /// <returns>Vrací <c>true</c>, pokud bylo parsování úspěšné; jinak <c>false</c>.</returns>
        private bool TryParseAccountAndIp(string input, out int account, out string ip, out string errorMsg)
        {
            //Inicializace výstupních proměnných
            account = 0;
            ip = "";
            errorMsg = "";

            // Kontrola, zda vstupní řetězec není prázdný nebo složen pouze z mezer
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMsg = "Prázdný vstup pro account/ip.";
                return false;
            }

            // Rozdělení řetězce podle lomítka
            string[] split = input.Split('/');
            if (split.Length != 2)
            {
                errorMsg = "Formát čísla účtu není správný (chybí lomítko).";
                return false;
            }

            // Pokus o převod první části na číslo účtu a kontrola, zda je v povoleném rozsahu
            if (!int.TryParse(split[0], out account) || account < 10000 || account > 99999)
            {
                errorMsg = "Formát čísla účtu není správný (není v rozsahu 10000 až 99999).";
                return false;
            }

            // Přiřazení druhé části jako IP adresy
            ip = split[1];
            // Kontrola formátu IP adresy
            if (!IsValidIp(ip))
            {
                errorMsg = "Formát IP adresy není správný.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Ověří, zda zadaný řetězec odpovídá formátu IP adresy (x.x.x.x, kde každá hodnota je mezi 0 a 255).
        /// </summary>
        /// <param name="ipAddress">Řetězec obsahující IP adresu.</param>
        /// <returns>Vrací <c>true</c>, pokud je formát IP adresy správný; jinak <c>false</c>.</returns>
        private bool IsValidIp(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out _);
        }
    }
}
