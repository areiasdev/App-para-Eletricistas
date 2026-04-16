using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Common.Interfaces;

public interface IPlanGateService
{
    /// <summary>Returns max number of clients. -1 means unlimited.</summary>
    int MaxClients(Plan plan);

    /// <summary>Returns max quotes per month. -1 means unlimited.</summary>
    int MaxQuotesPerMonth(Plan plan);

    /// <summary>Returns max team members (excluding owner). -1 means unlimited.</summary>
    int MaxTeamMembers(Plan plan);

    bool CanUseDigitalSignature(Plan plan);
    bool CanUseTeam(Plan plan);
    bool CanUseMaterials(Plan plan);
    bool CanUseAdvancedReports(Plan plan);
}
