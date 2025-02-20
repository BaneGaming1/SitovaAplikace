using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SíťovýProjekt.Src
{
    /// <summary>
    /// BankAccount reprezentuje bankovní účet.
    /// Mapuje se na tabulku BankAccounts v databázi.
    /// Každá instance této třídy odpovídá jednomu záznamu (účtu) v databázi.
    /// </summary>
    public class BankAccount : IValidatableObject
    {
        /// <summary>
        /// Primární klíč tabulky BankAccounts.
        /// Číslo účtu (AccountID) musí být v rozsahu 10000 až 99999.
        /// Použití atributu <see cref="Key"/> označuje tuto vlastnost jako primární klíč.
        /// Atribut <see cref="RangeAttribute"/> zajistí, že hodnota bude v daném rozsahu.
        /// </summary>
        [Key]
        [Range(10000, 99999, ErrorMessage = "Číslo účtu musí být v rozsahu 10000 až 99999.")]
        public int AccountID { get; set; }

        /// <summary>
        /// Zůstatek na účtu.
        /// Hodnota musí být nezáporná.
        /// Atribut <see cref="RequiredAttribute"/> zajišťuje, že hodnota musí být zadána.
        /// Atribut <see cref="RangeAttribute"/> kontroluje, že hodnota je minimálně 0.
        /// </summary>
        [Required(ErrorMessage = "Zůstatek je povinný.")]
        [Range(0, long.MaxValue, ErrorMessage = "Zůstatek musí být nezáporný.")]
        public long Balance { get; set; }

        /// <summary>
        /// Ruční validace dat třídy <c>BankAccount</c>.
        /// Tato metoda je součástí implementace rozhraní <see cref="IValidatableObject"/>.
        /// Umožňuje provést vlastní kontrolu, zda jsou data platná, nad rámec standardních datových anotací.
        /// </summary>
        /// <param name="validationContext">Kontext validace, který obsahuje informace o objektu, který se validuje.</param>
        /// <returns>Seznam chyb při validaci, pokud existují. Pokud je seznam prázdný, data jsou platná.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Kontrola, zda je číslo účtu v povoleném rozsahu (10000 až 99999)
            if (AccountID < 10000 || AccountID > 99999)
            {
                // Vrátí se chyba validace, pokud číslo účtu nesplňuje podmínku
                yield return new ValidationResult(
                    "Číslo účtu musí být v rozsahu 10000 až 99999.",
                    new[] { nameof(AccountID) } // Specifikuje, která vlastnost je chybně nastavena
                );
            }

            // Kontrola, zda je zůstatek nezáporný
            if (Balance < 0)
            {
                // Vrátí se chyba validace, pokud je zůstatek záporný
                yield return new ValidationResult(
                    "Zůstatek musí být nezáporný.",
                    new[] { nameof(Balance) } // Specifikuje, že problém je u vlastnosti Balance
                );
            }
        }
    }
}
