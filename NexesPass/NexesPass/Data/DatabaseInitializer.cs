using Microsoft.EntityFrameworkCore;
using NexesPass.Models;

namespace NexesPass.Data;

public static class DatabaseInitializer
{
    public static void Initialize(BankDbContext context)
    {
        context.Database.EnsureCreated();
        SeedExchangeRates(context);
    }

    private static void SeedExchangeRates(BankDbContext context)
    {
        if (context.ExchangeRates.Any()) return;

        var rates = new[]
        {
            new ExchangeRate { FromCurrency = Currency.USD, ToCurrency = Currency.RUB, Rate = 91.50m },
            new ExchangeRate { FromCurrency = Currency.EUR, ToCurrency = Currency.RUB, Rate = 99.80m },
            new ExchangeRate { FromCurrency = Currency.GBP, ToCurrency = Currency.RUB, Rate = 116.30m },
            new ExchangeRate { FromCurrency = Currency.CNY, ToCurrency = Currency.RUB, Rate = 12.65m },
            new ExchangeRate { FromCurrency = Currency.RUB, ToCurrency = Currency.USD, Rate = 0.0109m },
            new ExchangeRate { FromCurrency = Currency.RUB, ToCurrency = Currency.EUR, Rate = 0.0100m },
            new ExchangeRate { FromCurrency = Currency.RUB, ToCurrency = Currency.GBP, Rate = 0.0086m },
            new ExchangeRate { FromCurrency = Currency.USD, ToCurrency = Currency.EUR, Rate = 0.921m },
            new ExchangeRate { FromCurrency = Currency.EUR, ToCurrency = Currency.USD, Rate = 1.086m },
        };

        context.ExchangeRates.AddRange(rates);
        context.SaveChanges();
    }
}
