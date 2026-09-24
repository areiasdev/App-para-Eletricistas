using System.Text;
using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TecnicoApp.Application.Common.Extensions;
using TecnicoApp.Application.Common.Formatting;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Features.Invoices.Queries.ExportInvoices;

/// <summary>Invoices issued in [From, To] as a CSV the accountant can open in Excel.</summary>
public record ExportInvoicesCsvQuery(DateTime? From, DateTime? To) : IRequest<Result<byte[]>>;

public class ExportInvoicesCsvQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ExportInvoicesCsvQuery, Result<byte[]>>
{
    private static readonly Dictionary<InvoiceStatus, string> StatusLabels = new()
    {
        [InvoiceStatus.Issued] = "Emitida",
        [InvoiceStatus.Paid] = "Paga",
        [InvoiceStatus.Overdue] = "Vencida",
        [InvoiceStatus.Cancelled] = "Anulada",
    };

    public async Task<Result<byte[]>> Handle(ExportInvoicesCsvQuery request, CancellationToken cancellationToken)
    {
        var caller = await db.ResolveCallerAsync(currentUser.UserId, cancellationToken);
        if (caller is null) return Result.Unauthorized();
        if (caller.Role is not (UserRole.Owner or UserRole.Admin))
            return Result.Forbidden("Apenas o proprietário ou administradores podem exportar faturas.");

        var from = (request.From ?? new DateTime(DateTime.UtcNow.Year, 1, 1)).Date;
        var to = (request.To ?? DateTime.UtcNow).Date.AddDays(1);

        var invoices = await db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Client)
            .Where(i => i.UserId == caller.OwnerId && i.IssuedAt >= from && i.IssuedAt < to)
            .OrderBy(i => i.IssuedAt)
            .ToListAsync(cancellationToken);

        // Portuguese Excel expects ";" as separator and "," as decimal mark; the UTF-8 BOM makes
        // it read accents correctly instead of guessing Windows-1252.
        var csv = new StringBuilder();
        csv.AppendLine("Número;Data;Vencimento;Estado;Cliente;NIF cliente;Base tributável;IVA;Desconto;Total;Pago em");
        foreach (var i in invoices)
        {
            csv.AppendJoin(';',
                Field(i.Number),
                PtFormat.ShortDate(i.IssuedAt),
                PtFormat.ShortDate(i.DueDate),
                StatusLabels[i.Status],
                Field(i.Client.Name),
                Field(i.Client.Nif),
                Amount(i.SubTotal),
                Amount(i.VatTotal),
                Amount(i.Discount ?? 0),
                Amount(i.Total),
                i.PaidAt.HasValue ? PtFormat.ShortDate(i.PaidAt.Value) : "");
            csv.AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return Result.Success(bytes);
    }

    private static string Amount(decimal value) => value.ToString("0.00", PtFormat.Culture);

    // Quote fields that could contain the separator/quotes/newlines; neutralise leading
    // =,+,-,@ so a client name can't become a spreadsheet formula (CSV injection).
    private static string Field(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if ("=+-@".Contains(value[0])) value = "'" + value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
