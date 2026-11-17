using Backend.Authorization;
using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Backend.Models.Enums;

using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class SignatureCompletenessService(AppDbContext appDbContext)
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public async Task<SignatureCompletenessReportDto> GetReportAsync()
    {
        var computedAt = DateTime.UtcNow;

        // Operators on shift = role Operator and working status AtWork.
        // Roles come from UserRoleAssignments, not from a column on the user.
        var operatorsOnShift = await (
            from user in _appDbContext.Users
            join assignment in _appDbContext.UserRoleAssignments
                on user.Id equals assignment.UserId
            join role in _appDbContext.Roles
                on assignment.RoleId equals role.Id
            where role.Name == RoleNames.Operator
                && user.WorkingStatus == UserWorkingStatus.AtWork
            select user
        )
            .AsNoTracking()
            .OrderBy(user => user.Name)
            .ToListAsync();

        var allRestrictions = await _appDbContext.OperationalRestrictions
            .AsNoTracking()
            .Include(restriction => restriction.SignatureSheet)
            .ToListAsync();

        var restrictionsInForce = allRestrictions
            .GroupBy(restriction => restriction.OriginalOperationalRestrictionId ?? restriction.RestrictionId)
            .Select(group => group.OrderByDescending(version => version.Version).First())
            .Where(restriction => restriction.State == RestrictionState.Active)
            .OrderBy(restriction => restriction.StartDate)
            .ThenBy(restriction => restriction.Title)
            .ToList();

        var restrictions = restrictionsInForce
            .Select(restriction => new RestrictionInForceDto
            {
                RestrictionId = restriction.RestrictionId,
                Title = restriction.Title,
                Reference = restriction.Reference
            })
            .ToList();

        var operators = operatorsOnShift
            .Select(operatorUser => new OperatorSignatureStatusDto
            {
                OperatorId = operatorUser.Id,
                Name = operatorUser.Name,
                Cells = restrictionsInForce
                    .Select(restriction => ToCell(restriction, operatorUser.Id))
                    .ToList()
            })
            .ToList();

        return new SignatureCompletenessReportDto
        {
            ComputedAt = computedAt,
            OutstandingSignatureCount = operators.Sum(operatorRow => operatorRow.Cells.Count(cell => !cell.IsSigned)),
            Restrictions = restrictions,
            Operators = operators
        };
    }

    private static SignatureCellDto ToCell(OperationalRestriction restriction, Guid operatorId)
    {
        var signature = restriction.SignatureSheet
            .FirstOrDefault(s => s.SigneeId == operatorId);

        return new SignatureCellDto
        {
            RestrictionId = restriction.RestrictionId,
            IsSigned = signature != null,
            SignedAt = signature?.SignedAt
        };
    }
}
