using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.EntityFrameworkCore.SqlServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SíťovýProjekt.Src
{
    /// <summary>
    /// Kontext databáze pro bankovní účty.
    /// Tato třída konfiguruje připojení k databázi pomocí proměnných z .env souboru
    /// a mapuje entitu <c>BankAccount</c> na tabulku <c>BankAccounts</c> v databázi.
    /// </summary>
    public class BankContext : DbContext
    {
        /// <summary>
        /// DbSet pro entitu BankAccount.
        /// Tento DbSet umožňuje provádět operace (CRUD) nad entitou BankAccount,
        /// která je mapována na tabulku BankAccounts.
        /// </summary>
        public DbSet<BankAccount> BankAccounts { get; set; }

        /// <summary>
        /// Konfigurace připojení k databázi.
        /// Metoda načte proměnné z .env souboru, sestaví connection string a předá jej do EF Core.
        /// </summary>
        /// <param name="optionsBuilder">Objekt pro konfiguraci možností připojení k databázi.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Pokud connection string ještě nebyl nakonfigurován
            if (!optionsBuilder.IsConfigured)
            {
                // Načtení proměnných z .env souboru
                Env.Load();

                // Načtení jednotlivých připojovacích údajů z environmentálních proměnných
                var server = Environment.GetEnvironmentVariable("DB_SERVER");
                var dbName = Environment.GetEnvironmentVariable("DB_NAME");
                var user = Environment.GetEnvironmentVariable("DB_USER");
                var pass = Environment.GetEnvironmentVariable("DB_PASS");

                // Sestavení connection stringu pomocí SqlConnectionStringBuilder
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = server,          // Adresa serveru, např. (V mém případě adresa mého školního MS SQL Serveru)
                    InitialCatalog = dbName,       // Název databáze
                    IntegratedSecurity = false,    // Používá se SQL autentizace (nikoliv Windows)
                    UserID = user,                 // Uživatelské jméno pro přihlášení
                    Password = pass,               // Heslo pro přihlášení
                    TrustServerCertificate = true  // Ignorování ověřování certifikátu (pokud je to potřeba)
                };

                // Předání sestaveného connection stringu do EF Core
                optionsBuilder.UseSqlServer(builder.ToString());
            }
        }

        /// <summary>
        /// Konfigurace mapování entit na databázové tabulky.
        /// Tato metoda definuje, jak se entita BankAccount mapuje na tabulku BankAccounts.
        /// </summary>
        /// <param name="modelBuilder">Objekt ModelBuilder, který se používá k konfiguraci modelu databáze.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konfigurace mapování pro entitu BankAccount
            modelBuilder.Entity<BankAccount>(entity =>
            {
                // Mapování entity na tabulku s názvem "BankAccounts"
                entity.ToTable("BankAccounts");

                // Definice primárního klíče – vlastnost AccountID je primární klíč
                entity.HasKey(e => e.AccountID);

                // Nastavení, že vlastnost AccountID je povinná (nesmí být null)
                entity.Property(e => e.AccountID).IsRequired();

                // Nastavení, že vlastnost Balance je povinná (nesmí být null)
                entity.Property(e => e.Balance).IsRequired();

                // Přidání kontrolního omezení pro AccountID, které zajišťuje, že číslo účtu je v rozsahu 10000 až 99999.
                entity.HasCheckConstraint("CK_BankAccounts_AccountID", "[AccountID] BETWEEN 10000 AND 99999");

                //Kontrolní omezení pro Balance, které zajišťuje, že zůstatek je nezáporný.
                entity.HasCheckConstraint("CK_BankAccounts_Balance", "[Balance] >= 0");
            });

            // Zavolání základní implementace OnModelCreating
            base.OnModelCreating(modelBuilder);
        }
    }
}
