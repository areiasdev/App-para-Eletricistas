using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Enums;

namespace TecnicoApp.Application.Common.Services;

public class PlanGateService : IPlanGateService
{
    public int MaxClients(Plan plan) => plan switch
    {
        Plan.Free => 5,
        _ => -1
    };

    public int MaxQuotesPerMonth(Plan plan) => plan switch
    {
        Plan.Free => 10,
        _ => -1
    };

    public int MaxTeamMembers(Plan plan) => plan switch
    {
        Plan.Free => 0,
        Plan.Pro => 0,
        Plan.Team => 4,
        Plan.Enterprise => -1,
        _ => 0
    };

    public bool CanUseDigitalSignature(Plan plan)
        => plan is Plan.Pro or Plan.Team or Plan.Enterprise;

    public bool CanUseTeam(Plan plan)
        => plan is Plan.Team or Plan.Enterprise;

    public bool CanUseMaterials(Plan plan)
        => plan is Plan.Enterprise;

    public bool CanUseAdvancedReports(Plan plan)
        => plan is Plan.Enterprise;
}
