using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SíťovýProjekt.Src
{
    /// <summary>
    /// Logger slouží k logování zpráv do konzole a do souboru.
    /// Umožňuje logování na úrovních DEBUG, INFO, WARNING a ERROR.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Výčet logovacích úrovní.
        /// </summary>
        public enum LogLevel { DEBUG, INFO, WARNING, ERROR }

        // Soukromá proměnná, která uchovává minimální logovací úroveň. 
        // Zprávy s úrovní nižší než tato se nebudou logovat.
        private readonly LogLevel _logLevel;

        // Cesta k souboru, do kterého se budou zapisovat logovací zprávy.
        private readonly string _logFile;

        /// <summary>
        /// Konstruktor třídy Logger.
        /// Inicializuje Logger s cestou k logovacímu souboru a výchozí logovací úrovní.
        /// </summary>
        /// <param name="logFile">Cesta k souboru, do kterého se budou zapisovat logy.</param>
        /// <param name="level">Minimální logovací úroveň, výchozí je INFO.</param>
        public Logger(string logFile, LogLevel level = LogLevel.INFO)
        {
            _logLevel = level;
            _logFile = logFile;
        }

        /// <summary>
        /// Soukromá metoda, která provádí samotné logování zprávy.
        /// Pokud je zadaná úroveň větší nebo rovna nastavené minimální úrovni,
        /// zpráva je vypsána do konzole a připojena do logovacího souboru.
        /// </summary>
        /// <param name="level">Úroveň logu (DEBUG, INFO, WARNING, ERROR).</param>
        /// <param name="message">Text zprávy, která se má zalogovat.</param>
        private void Log(LogLevel level, string message)
        {
            // Zkontroluje, zda aktuální úroveň zprávy je vyšší nebo rovna minimální nastavené úrovni
            if (level >= _logLevel)
            {
                // Sestaví text logovací zprávy včetně časové značky a úrovně logu
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

                // Vypíše zprávu do konzole
                Console.WriteLine(logMessage);

                // Připojí zprávu do souboru, přidá nový řádek
                File.AppendAllText(_logFile, logMessage + "\n");
            }
        }

        /// <summary>
        /// Loguje zprávu na úrovni INFO.
        /// </summary>
        /// <param name="message">Text zprávy, která se má zalogovat.</param>
        public void Info(string message)
        {
            Log(LogLevel.INFO, message);
        }

        /// <summary>
        /// Loguje zprávu na úrovni DEBUG.
        /// </summary>
        /// <param name="message">Text zprávy, která se má zalogovat.</param>
        public void Debug(string message)
        {
            Log(LogLevel.DEBUG, message);
        }

        /// <summary>
        /// Loguje zprávu na úrovni ERROR.
        /// </summary>
        /// <param name="message">Text zprávy, která se má zalogovat.</param>
        public void Error(string message)
        {
            Log(LogLevel.ERROR, message);
        }

        /// <summary>
        /// Loguje zprávu na úrovni WARNING.
        /// </summary>
        /// <param name="message">Text zprávy, která se má zalogovat.</param>
        public void Warning(string message)
        {
            Log(LogLevel.WARNING, message);
        }
    }
}
