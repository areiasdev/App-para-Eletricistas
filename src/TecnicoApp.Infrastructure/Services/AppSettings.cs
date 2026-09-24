using Microsoft.Extensions.Configuration;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Infrastructure.Services;

public class AppSettings(IConfiguration configuration) : IAppSettings
{
    public const string DefaultProductName = "TécnicoApp";
    public const int DefaultInvoicePaymentTermDays = 30;

    public string BaseUrl => (configuration["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');

    public string ProductName =>
        string.IsNullOrWhiteSpace(configuration["App:ProductName"])
            ? DefaultProductName
            : configuration["App:ProductName"]!;

    public int InvoicePaymentTermDays =>
        int.TryParse(configuration["App:InvoicePaymentTermDays"], out var days) && days >= 0
            ? days
            : DefaultInvoicePaymentTermDays;

    public bool AllowOpenRegistration =>
        bool.TryParse(configuration["App:AllowOpenRegistration"], out var allow) && allow;
}
