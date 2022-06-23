using CatenaX.NetworkServices.Framework.Models;
using CatenaX.NetworkServices.PortalBackend.DBAccess.Models;
using CatenaX.NetworkServices.PortalBackend.PortalEntities;
using CatenaX.NetworkServices.PortalBackend.PortalEntities.Entities;
using CatenaX.NetworkServices.PortalBackend.PortalEntities.Enums;
using Microsoft.EntityFrameworkCore;

namespace CatenaX.NetworkServices.PortalBackend.DBAccess.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly PortalDbContext _dbContext;

    public ApplicationRepository(PortalDbContext portalDbContext)
    {
        _dbContext = portalDbContext;
    }

    public CompanyApplication CreateCompanyApplication(Company company, CompanyApplicationStatusId companyApplicationStatusId) =>
        _dbContext.CompanyApplications.Add(
            new CompanyApplication(
                Guid.NewGuid(),
                company.Id,
                companyApplicationStatusId,
                DateTimeOffset.UtcNow)).Entity;

    public Invitation CreateInvitation(Guid applicationId, CompanyUser user) =>
        _dbContext.Invitations.Add(
            new Invitation(
                Guid.NewGuid(),
                applicationId,
                user.Id,
                InvitationStatusId.CREATED,
                DateTimeOffset.UtcNow)).Entity;

    public Task<CompanyWithAddress?> GetCompanyWithAdressUntrackedAsync(Guid companyApplicationId) =>
        _dbContext.CompanyApplications
            .Where(companyApplication => companyApplication.Id == companyApplicationId)
            .Select(
                companyApplication => new CompanyWithAddress(
                    companyApplication.CompanyId,
                    companyApplication.Company!.Name,
                    companyApplication.Company.Address!.City ?? "",
                    companyApplication.Company.Address.Streetname ?? "",
                    companyApplication.Company.Address.CountryAlpha2Code ?? "")
                {
                    BusinessPartnerNumber = companyApplication.Company!.BusinessPartnerNumber,
                    Shortname = companyApplication.Company.Shortname,
                    Region = companyApplication.Company.Address.Region,
                    Streetadditional = companyApplication.Company.Address.Streetadditional,
                    Streetnumber = companyApplication.Company.Address.Streetnumber,
                    Zipcode = companyApplication.Company.Address.Zipcode,
                    CountryDe = companyApplication.Company.Address.Country!.CountryNameDe, // FIXME internationalization, maybe move to separate endpoint that returns Contrynames for all (or a specific) language
                    TaxId = companyApplication.Company.TaxId
                })
            .AsNoTracking()
            .SingleOrDefaultAsync();

    public IQueryable<CompanyApplication> GetCompanyApplicationDetailsUntrackedAsync(string? companyName = null) =>
        _dbContext.Companies
               .AsNoTracking()
               .Where(company => (companyName != null && companyName.Length >= 3) ? company!.Name.StartsWith(companyName) : true)
               .SelectMany(company => company.CompanyApplications);

    public Task<CompanyApplication?> GetCompanyAndApplicationForSubmittedApplication(Guid applicationId) =>
        _dbContext.CompanyApplications.Where(companyApplication =>
            companyApplication.Id == applicationId
            && companyApplication.ApplicationStatusId == CompanyApplicationStatusId.SUBMITTED)
            .Include(companyApplication => companyApplication.Company)
            .SingleOrDefaultAsync();

    public IAsyncEnumerable<CompanyInvitedUserData> GetInvitedUsersDataByApplicationIdUntrackedAsync(Guid applicationId) =>
        _dbContext.Invitations
            .AsNoTracking()
            .Where(invitation => invitation.CompanyApplicationId == applicationId)
            .Select(invitation => invitation.CompanyUser)
            .Where(companyUser => companyUser!.CompanyUserStatusId == CompanyUserStatusId.ACTIVE)
            .Select(companyUser => new CompanyInvitedUserData(
                companyUser!.Id,
                companyUser.IamUser!.UserEntityId,
                companyUser.CompanyUserAssignedBusinessPartners.Select(companyUserAssignedBusinessPartner => companyUserAssignedBusinessPartner.BusinessPartnerNumber),
                companyUser.CompanyUserAssignedRoles.Select(companyUserAssignedRole => companyUserAssignedRole.UserRoleId)))
            .AsAsyncEnumerable();

    public IAsyncEnumerable<WelcomeEmailData> GetWelcomeEmailDataUntrackedAsync(Guid applicationId) =>
        _dbContext.CompanyApplications
            .AsNoTracking()
            .Where(application => application.Id == applicationId)
            .SelectMany(application =>
                application.Company!.CompanyUsers
                    .Where(companyUser => companyUser.CompanyUserStatusId == CompanyUserStatusId.ACTIVE)
                    .Select(companyUser => new WelcomeEmailData(
                        companyUser.Firstname,
                        companyUser.Lastname,
                        companyUser.Email,
                        companyUser.Company!.Name)))
            .AsAsyncEnumerable();

}
