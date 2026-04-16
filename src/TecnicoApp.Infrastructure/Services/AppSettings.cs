using Microsoft.Extensions.Configuration;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Infrastructure.Services;

public class AppSettings(IConfiguration configuration) : IAppSettings
{
    public string BaseUrl => configuration["App:BaseUrl"] ?? "http://localhost:3000";
}
