/********************************************************************************
 * Copyright (c) 2021, 2023 BMW Group AG
 * Copyright (c) 2021, 2023 Contributors to the Eclipse Foundation
 *
 * See the NOTICE file(s) distributed with this work for additional
 * information regarding copyright ownership.
 *
 * This program and the accompanying materials are made available under the
 * terms of the Apache License, Version 2.0 which is available at
 * https://www.apache.org/licenses/LICENSE-2.0.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 * SPDX-License-Identifier: Apache-2.0
 ********************************************************************************/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Org.Eclipse.TractusX.Portal.Backend.Apps.Service.Extensions;
using Org.Eclipse.TractusX.Portal.Backend.Apps.Service.ViewModels;
using Org.Eclipse.TractusX.Portal.Backend.Framework.ErrorHandling;
using Org.Eclipse.TractusX.Portal.Backend.Framework.IO;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Models;
using Org.Eclipse.TractusX.Portal.Backend.Offers.Library.Models;
using Org.Eclipse.TractusX.Portal.Backend.Offers.Library.Service;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Repositories;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;
using PortalBackend.DBAccess.Models;

namespace Org.Eclipse.TractusX.Portal.Backend.Apps.Service.BusinessLogic;

/// <summary>
/// Implementation of <see cref="IAppReleaseBusinessLogic"/>.
/// </summary>
public class AppReleaseBusinessLogic : IAppReleaseBusinessLogic
{
    private readonly IPortalRepositories _portalRepositories;
    private readonly AppsSettings _settings;
    private readonly IOfferService _offerService;
    private readonly IOfferSetupService _offerSetupService;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="portalRepositories"></param>
    /// <param name="settings"></param>
    /// <param name="offerService"></param>
    /// <param name="offerSetupService"></param>
    public AppReleaseBusinessLogic(IPortalRepositories portalRepositories, IOptions<AppsSettings> settings, IOfferService offerService, IOfferSetupService offerSetupService)
    {
        _portalRepositories = portalRepositories;
        _settings = settings.Value;
        _offerService = offerService;
        _offerSetupService = offerSetupService;
    }
    
    /// <inheritdoc/>
    public  Task UpdateAppAsync(Guid appId, AppEditableDetail updateModel, string userId)
    {
        if (appId == Guid.Empty)
        {
            throw new ControllerArgumentException($"AppId must not be empty");
        }
        if (updateModel.Descriptions.Any(item => string.IsNullOrWhiteSpace(item.LanguageCode)))
        {
            throw new ControllerArgumentException("Language Code must not be empty");
        }
        return EditAppAsync(appId, updateModel, userId);
    }

    private async Task EditAppAsync(Guid appId, AppEditableDetail updateModel, string userId)
    {
        var appRepository = _portalRepositories.GetInstance<IOfferRepository>();
        var appResult = await appRepository.GetOfferDetailsForUpdateAsync(appId, userId, OfferTypeId.APP).ConfigureAwait(false);
        if (appResult == default)
        {
            throw new NotFoundException($"app {appId} does not exist");
        }
        if (!appResult.IsProviderUser)
        {
            throw new ForbiddenException($"user {userId} is not eligible to edit app {appId}");
        }
        if (!appResult.IsAppCreated)
        {
            throw new ConflictException($"app {appId} is not in status CREATED");
        }
        appRepository.AttachAndModifyOffer(appId, app =>
        {
            if (appResult.ContactEmail != updateModel.ContactEmail)
            {
                app.ContactEmail = updateModel.ContactEmail;
            }
            if (appResult.ContactNumber != updateModel.ContactNumber)
            {
                app.ContactNumber = updateModel.ContactNumber;
            }
            if (appResult.MarketingUrl != updateModel.ProviderUri)
            {
                app.MarketingUrl = updateModel.ProviderUri;
            }
        });

        _offerService.UpsertRemoveOfferDescription(appId, updateModel.Descriptions, appResult.Descriptions);
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task CreateAppDocumentAsync(Guid appId, DocumentTypeId documentTypeId, IFormFile document, string iamUserId, CancellationToken cancellationToken) =>
        UploadAppDoc(appId, documentTypeId, document, iamUserId, OfferTypeId.APP, cancellationToken);
    
    private async Task UploadAppDoc (Guid appId, DocumentTypeId documentTypeId, IFormFile document, string iamUserId, OfferTypeId offerTypeId, CancellationToken cancellationToken) =>
        await _offerService.UploadDocumentAsync(appId, documentTypeId, document, iamUserId, offerTypeId, _settings.UploadAppDocumentTypeIds, cancellationToken).ConfigureAwait(false);
    
    /// <inheritdoc/>
    public Task<IEnumerable<AppRoleData>> AddAppUserRoleAsync(Guid appId, IEnumerable<AppUserRole> userRoles, string iamUserId)
    {
        AppExtensions.ValidateAppUserRole(appId, userRoles);
        return InsertAppUserRoleAsync(appId, userRoles, iamUserId);
    }

    private async Task<IEnumerable<AppRoleData>> InsertAppUserRoleAsync(Guid appId, IEnumerable<AppUserRole> userRoles, string iamUserId)
    {
        var result = await _portalRepositories.GetInstance<IOfferRepository>().IsProviderCompanyUserAsync(appId, iamUserId, OfferTypeId.APP).ConfigureAwait(false);
        if (result == default)
        {
            throw new NotFoundException($"app {appId} does not exist");
        }
        if (!result.IsProviderCompanyUser)
        {
            throw new ForbiddenException($"user {iamUserId} is not a member of the providercompany of app {appId}");
        }
        var roleData = AppExtensions.CreateUserRolesWithDescriptions(_portalRepositories.GetInstance<IUserRolesRepository>(), appId, userRoles);
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
        return roleData;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<AgreementDocumentData> GetOfferAgreementDataAsync()=>
        _offerService.GetOfferTypeAgreements(OfferTypeId.APP);

    /// <inheritdoc/>
    public async Task<OfferAgreementConsent> GetOfferAgreementConsentById(Guid appId, string userId)
    {
        return await _offerService.GetProviderOfferAgreementConsentById(appId, userId, OfferTypeId.APP).ConfigureAwait(false);
    }
    
    /// <inheritdoc/>
    public Task<IEnumerable<ConsentStatusData>> SubmitOfferConsentAsync(Guid appId, OfferAgreementConsent offerAgreementConsents, string userId)
    {
        if (appId == Guid.Empty)
        {
            throw new ControllerArgumentException($"AppId must not be empty");
        }
        return SubmitOfferConsentInternalAsync(appId, offerAgreementConsents, userId);
    }

    /// <inheritdoc/>
    private Task<IEnumerable<ConsentStatusData>> SubmitOfferConsentInternalAsync(Guid appId, OfferAgreementConsent offerAgreementConsents, string userId) =>
        _offerService.CreateOrUpdateProviderOfferAgreementConsent(appId, offerAgreementConsents, userId, OfferTypeId.APP);
    
    /// <inheritdoc/>
    public async Task<AppProviderResponse> GetAppDetailsForStatusAsync(Guid appId, string userId)
    {
        var result = await _offerService.GetProviderOfferDetailsForStatusAsync(appId, userId, OfferTypeId.APP).ConfigureAwait(false);
        if (result.UseCase == null)
        {
            throw new UnexpectedConditionException("usecase should never be null here");
        }
        return new AppProviderResponse(
            result.Title,
            result.Provider,
            result.LeadPictureId,
            result.ProviderName,
            result.UseCase,
            result.Descriptions,
            result.Agreements,
            result.SupportedLanguageCodes,
            result.Price,
            result.Images,
            result.ProviderUri,
            result.ContactEmail,
            result.ContactNumber,
            result.Documents,
            result.SalesManagerId,
            result.PrivacyPolicies);
    }

    /// <inheritdoc/>
    public async Task DeleteAppRoleAsync(Guid appId, Guid roleId, string iamUserId)
    {
        var appUserRole = await _portalRepositories.GetInstance<IOfferRepository>().GetAppUserRoleUntrackedAsync(appId, iamUserId, OfferStatusId.CREATED, roleId).ConfigureAwait(false);
        if (!appUserRole.IsProviderCompanyUser)
        {
            throw new ForbiddenException($"user {iamUserId} is not a member of the providercompany of app {appId}");
        }
        if (!appUserRole.OfferStatus)
        {
            throw new ControllerArgumentException($"AppId must be in Created State");
        }
        if (!appUserRole.IsRoleIdExist)
        {
            throw new NotFoundException($"role {roleId} does not exist");
        }
        try
        {
            _portalRepositories.GetInstance<IUserRolesRepository>().DeleteUserRole(roleId);
            await _portalRepositories.SaveAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new NotFoundException($"role {roleId} does not exist");
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<CompanyUserNameData> GetAppProviderSalesManagersAsync(string iamUserId) =>
       _portalRepositories.GetInstance<IUserRolesRepository>().GetUserDataByAssignedRoles(iamUserId,_settings.SalesManagerRoles);
    
    /// <inheritdoc/>
    public Task<Guid> AddAppAsync(AppRequestModel appRequestModel, string iamUserId)
    {
        var emptyLanguageCodes = appRequestModel.SupportedLanguageCodes.Where(string.IsNullOrWhiteSpace);
        if (emptyLanguageCodes.Any())
        {
            throw new ControllerArgumentException("Language Codes must not be null or empty", nameof(appRequestModel.SupportedLanguageCodes)); 
        }
        
        var emptyUseCaseIds = appRequestModel.UseCaseIds.Where(item => item == Guid.Empty);
        if (emptyUseCaseIds.Any())
        {
            throw new ControllerArgumentException("Use Case Ids must not be null or empty", nameof(appRequestModel.UseCaseIds));
        }

        return this.CreateAppAsync(appRequestModel, iamUserId);
    }

    private async Task<Guid> CreateAppAsync(AppRequestModel appRequestModel, string iamUserId)
    {   
        Guid companyId;
        if(appRequestModel.SalesManagerId.HasValue)
        {
            companyId = await _offerService.ValidateSalesManager(appRequestModel.SalesManagerId.Value, iamUserId, _settings.SalesManagerRoles).ConfigureAwait(false);
        }
        else
        {
            companyId = await _portalRepositories.GetInstance<IUserRepository>()
                .GetOwnCompanyId(iamUserId)
                .ConfigureAwait(false);
            if (companyId == Guid.Empty)
            {
                throw new ControllerArgumentException($"user {iamUserId} is not associated with any company");
            }
        }

        var appRepository = _portalRepositories.GetInstance<IOfferRepository>();
        var appId = appRepository.CreateOffer(appRequestModel.Provider, OfferTypeId.APP, app =>
        {
            app.Name = appRequestModel.Title;
            app.ProviderCompanyId = companyId;
            app.OfferStatusId = OfferStatusId.CREATED;
            if (appRequestModel.SalesManagerId.HasValue)
            {
                app.SalesManagerId = appRequestModel.SalesManagerId;
            }
            app.ContactEmail = appRequestModel.ContactEmail;
            app.ContactNumber = appRequestModel.ContactNumber;
            app.MarketingUrl = appRequestModel.ProviderUri;
            app.LicenseTypeId = LicenseTypeId.COTS;
        }).Id;
        appRepository.AddOfferDescriptions(appRequestModel.Descriptions.Select(d =>
              (appId, d.LanguageCode, d.LongDescription, d.ShortDescription)));
        appRepository.AddAppLanguages(appRequestModel.SupportedLanguageCodes.Select(c =>
              (appId, c)));
        appRepository.AddAppAssignedUseCases(appRequestModel.UseCaseIds.Select(uc =>
              (appId, uc)));
        appRepository.AddAppAssignedPrivacyPolicies(appRequestModel.PrivacyPolicies.Select(pp =>
              (appId, pp)));
        var licenseId = appRepository.CreateOfferLicenses(appRequestModel.Price).Id;
        appRepository.CreateOfferAssignedLicense(appId, licenseId);

        try
        {
            await _portalRepositories.SaveAsync().ConfigureAwait(false);
            return appId;
        }
        catch(Exception exception)when (exception?.InnerException?.Message.Contains("violates foreign key constraint") ?? false)
        {
            throw new ControllerArgumentException($"invalid language code or UseCaseId specified");
        }
    }

    /// <inheritdoc/>
    public async Task UpdateAppReleaseAsync(Guid appId, AppRequestModel appRequestModel, string iamUserId)
    {
        var appData = await _portalRepositories.GetInstance<IOfferRepository>()
            .GetAppUpdateData(
                appId,
                iamUserId,
                appRequestModel.SupportedLanguageCodes)
            .ConfigureAwait(false);
        if (appData is null)
        {
            throw new NotFoundException($"App {appId} does not exists");
        }

        if (appData.OfferState != OfferStatusId.CREATED)
        {
            throw new ConflictException($"Apps in State {appData.OfferState} can't be updated");
        }

        if (!appData.IsUserOfProvider)
        {
            throw new ForbiddenException($"User {iamUserId} is not allowed to change the app.");
        }

        if (appRequestModel.SalesManagerId.HasValue)
        {
            await _offerService.ValidateSalesManager(appRequestModel.SalesManagerId.Value, iamUserId, _settings.SalesManagerRoles).ConfigureAwait(false);
        }

        var newSupportedLanguages = appRequestModel.SupportedLanguageCodes.Except(appData.Languages.Where(x => x.IsMatch).Select(x => x.Shortname));
        var existingLanguageCodes = await _portalRepositories.GetInstance<ILanguageRepository>().GetLanguageCodesUntrackedAsync(newSupportedLanguages).ToListAsync().ConfigureAwait(false);
        if (newSupportedLanguages.Except(existingLanguageCodes).Any())
        {
            throw new ControllerArgumentException($"The language(s) {string.Join(",", newSupportedLanguages.Except(existingLanguageCodes))} do not exist in the database.",
                nameof(appRequestModel.SupportedLanguageCodes));
        }

        var appRepository = _portalRepositories.GetInstance<IOfferRepository>();
        appRepository.AttachAndModifyOffer(
        appId,
        app =>
        {
            app.Name = appRequestModel.Title;
            app.Provider = appRequestModel.Provider;
            app.SalesManagerId = appRequestModel.SalesManagerId;
            app.ContactEmail = appRequestModel.ContactEmail;
            app.ContactNumber = appRequestModel.ContactNumber;
            app.MarketingUrl = appRequestModel.ProviderUri;
        },
        app => {
            app.Name = appData.Name;
            app.Provider = appData.Provider;
            app.SalesManagerId = appData.SalesManagerId;
            app.ContactEmail = appData.ContactEmail;
            app.ContactNumber = appData.ContactNumber;
            app.MarketingUrl = appData.MarketingUrl;
        });

        _offerService.UpsertRemoveOfferDescription(appId, appRequestModel.Descriptions, appData.OfferDescriptions);
        UpdateAppSupportedLanguages(appId, newSupportedLanguages, appData.Languages.Where(x => !x.IsMatch).Select(x => x.Shortname), appRepository);

        appRepository.CreateDeleteAppAssignedUseCases(appId, appData.MatchingUseCases, appRequestModel.UseCaseIds);

        appRepository.CreateDeleteAppAssignedPrivacyPolicies(appId, appData.MatchingPrivacyPolicies, appRequestModel.PrivacyPolicies);

        _offerService.CreateOrUpdateOfferLicense(appId, appRequestModel.Price, appData.OfferLicense);
        
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
    }

    private static void UpdateAppSupportedLanguages(Guid appId, IEnumerable<string> newSupportedLanguages, IEnumerable<string> languagesToRemove, IOfferRepository appRepository)
    {
        appRepository.AddAppLanguages(newSupportedLanguages.Select(language => (appId, language)));
        appRepository.RemoveAppLanguages(languagesToRemove.Select(language => (appId, language)));
    }

    /// <inheritdoc/>
    public Task<Pagination.Response<InReviewAppData>> GetAllInReviewStatusAppsAsync(int page, int size, OfferSorting? sorting, OfferStatusIdFilter? offerStatusIdFilter) =>
        Pagination.CreateResponseAsync(page, size, 15,
            _portalRepositories.GetInstance<IOfferRepository>()
                .GetAllInReviewStatusAppsAsync(GetOfferStatusIds(offerStatusIdFilter), sorting ?? OfferSorting.DateDesc));

    /// <inheritdoc/>
    public Task SubmitAppReleaseRequestAsync(Guid appId, string iamUserId) => 
        _offerService.SubmitOfferAsync(appId, iamUserId, OfferTypeId.APP, _settings.SubmitAppNotificationTypeIds, _settings.CatenaAdminRoles, _settings.SubmitAppDocumentTypeIds);
    
    /// <inheritdoc/>
    public Task ApproveAppRequestAsync(Guid appId, string iamUserId) =>
        _offerService.ApproveOfferRequestAsync(appId, iamUserId, OfferTypeId.APP, _settings.ApproveAppNotificationTypeIds, _settings.ApproveAppUserRoles, _settings.SubmitAppNotificationTypeIds, _settings.CatenaAdminRoles);
    
    private IEnumerable<OfferStatusId> GetOfferStatusIds(OfferStatusIdFilter? offerStatusIdFilter)
    {
        switch(offerStatusIdFilter)
        {
            case OfferStatusIdFilter.InReview :
            {
               return new []{ OfferStatusId.IN_REVIEW };
            }
            default :
            {
                return _settings.OfferStatusIds;
            }
        }       
    }

    /// <inheritdoc/>
    public  Task<PrivacyPolicyData> GetPrivacyPolicyDataAsync()
    {   
        return Task.FromResult(new PrivacyPolicyData(Enum.GetValues<PrivacyPolicyId>()));
    }

    /// <inheritdoc />
    public Task DeclineAppRequestAsync(Guid appId, string iamUserId, OfferDeclineRequest data) => 
        _offerService.DeclineOfferAsync(appId, iamUserId, data, OfferTypeId.APP, NotificationTypeId.APP_RELEASE_REJECTION, _settings.ServiceManagerRoles, _settings.AppOverviewAddress, _settings.SubmitAppNotificationTypeIds, _settings.CatenaAdminRoles);

    /// <inheritdoc />
    public async Task<InReviewAppDetails> GetInReviewAppDetailsByIdAsync(Guid appId)
    {
        var result = await _portalRepositories.GetInstance<IOfferRepository>()
            .GetInReviewAppDataByIdAsync(appId, OfferTypeId.APP).ConfigureAwait(false);
        
        if(result == default)
            throw new NotFoundException($"App {appId} not found or Incorrect Status");
        
        return new InReviewAppDetails(
            result.id,
            result.title ?? Constants.ErrorString,
            result.leadPictureId,
            result.images,
            result.Provider,
            result.UseCases,
            result.Description,
            result.Documents.GroupBy(d => d.documentTypeId).ToDictionary(g => g.Key, g => g.Select(d => new DocumentData(d.documentId, d.documentName))),
            result.Roles,
            result.Languages,
            result.ProviderUri ?? Constants.ErrorString,
            result.ContactEmail,
            result.ContactNumber,
            result.LicenseTypeId,
            result.Price ?? Constants.ErrorString,
            result.Tags,
            result.MatchingPrivacyPolicies);
    }

    /// <inheritdoc />
    public Task DeleteAppDocumentsAsync(Guid documentId, string iamUserId) =>
        _offerService.DeleteDocumentsAsync(documentId, iamUserId, _settings.DeleteDocumentTypeIds, OfferTypeId.APP);

    /// <inheritdoc />
    public async Task DeleteAppAsync(Guid appId, string iamUserId)
    {
        var (isValidApp, isOfferType, isOfferStatus, isProviderCompanyUser, appData) = await _portalRepositories.GetInstance<IOfferRepository>().GetAppDeleteDataAsync(appId, OfferTypeId.APP, iamUserId, OfferStatusId.CREATED).ConfigureAwait(false);
        if (!isValidApp)
        {
            throw new NotFoundException($"App {appId} does not exist");
        }
        if (!isProviderCompanyUser)
        {
            throw new ForbiddenException($"user {iamUserId} is not a member of the providercompany of app {appId}");
        }
        if (!isOfferStatus)
        {
            throw new ConflictException($"App {appId} is not in Created State");
        }
        if (!isOfferType)
        {
            throw new ConflictException($"offer {appId} is not offerType APP");
        }
        if (appData==null)
        {
            throw new UnexpectedConditionException("appData should never be null here");
        }
        _portalRepositories.GetInstance<IOfferRepository>().RemoveOfferAssignedLicenses(appData.OfferLicenseIds.Select(licenseId => (appId, licenseId)));
        _portalRepositories.GetInstance<IOfferRepository>().RemoveOfferAssignedUseCases(appData.UseCaseIds.Select(useCaseId => (appId, useCaseId)));
        _portalRepositories.GetInstance<IOfferRepository>().RemoveOfferAssignedPrivacyPolicies(appData.PolicyIds.Select(policyId => (appId, policyId)));
        _portalRepositories.GetInstance<IDocumentRepository>().RemoveDocuments(appData.DocumentIdStatus.Where(x => x.DocumentStatusId != DocumentStatusId.LOCKED).Select(x => x.DocumentId));
        _portalRepositories.GetInstance<IOfferRepository>().RemoveOfferAssignedDocuments(appData.DocumentIdStatus.Select(x => (appId, x.DocumentId)));
        _portalRepositories.GetInstance<IOfferRepository>().RemoveAppLanguages(appData.LanguageCodes.Select(language => (appId, language)));
        _portalRepositories.GetInstance<IOfferRepository>().RemoveOfferTags(appData.TagNames.Select(tagName => (appId, tagName)));
        _portalRepositories.GetInstance<IOfferRepository>().RemoveOfferDescriptions(appData.DescriptionLanguageShortNames.Select(languageShortName => (appId, languageShortName)));
        _portalRepositories.GetInstance<IOfferRepository>().RemoveOffer(appId);
        await _portalRepositories.SaveAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task SetInstanceType(Guid appId, AppInstanceSetupData data, string iamUserId)
    {
        switch (data.IsSingleInstance)
        {
            case true:
                if (string.IsNullOrWhiteSpace(data.InstanceUrl))
                {
                    throw new ControllerArgumentException("InstanceUrl must be set for a single instance app",
                        nameof(data.InstanceUrl));
                }
                data.InstanceUrl!.EnsureValidHttpUrl(() => nameof(data.InstanceUrl));
                break;

            case false when !string.IsNullOrWhiteSpace(data.InstanceUrl):
                throw new ControllerArgumentException("Multi instance app must not have a instance url set",
                    nameof(data.InstanceUrl));
        }

        return SetInstanceTypeInternal(appId, data, iamUserId);
    }

    private async Task SetInstanceTypeInternal(Guid appId, AppInstanceSetupData data, string iamUserId)
    {
        var result = await _portalRepositories.GetInstance<IOfferRepository>()
            .GetOfferWithSetupDataById(appId, iamUserId, OfferTypeId.APP)
            .ConfigureAwait(false);
        if (result == default)
            throw new NotFoundException($"App {appId} does not exist");

        if (!result.IsUserOfProvidingCompany)
            throw new ForbiddenException($"User {iamUserId} is not a user of the provider company");

        if (result.OfferStatus is not (OfferStatusId.CREATED or OfferStatusId.IN_REVIEW))
            throw new ConflictException($"App {appId} is not in Status {OfferStatusId.CREATED} or {OfferStatusId.IN_REVIEW}");

        await (result.SetupTransferData == null
            ? HandleAppInstanceCreation(appId, data)
            : HandleAppInstanceUpdate(appId, data, (result.OfferStatus, result.IsUserOfProvidingCompany, result.SetupTransferData, result.AppInstanceData))).ConfigureAwait(false);

        await _portalRepositories.SaveAsync().ConfigureAwait(false);
    }

    private async Task HandleAppInstanceCreation(Guid appId, AppInstanceSetupData data)
    {
        _portalRepositories.GetInstance<IOfferRepository>().CreateAppInstanceSetup(appId,
            entity =>
            {
                entity.IsSingleInstance = data.IsSingleInstance;
                entity.InstanceUrl = data.InstanceUrl;
            });
        
        if (data.IsSingleInstance)
        {
            await _offerSetupService
                .SetupSingleInstance(appId, data.InstanceUrl!).ConfigureAwait(false);
        }
    }

    private async Task HandleAppInstanceUpdate(
        Guid appId,
        AppInstanceSetupData data,
        (OfferStatusId OfferStatus, bool IsUserOfProvidingCompany, AppInstanceSetupTransferData SetupTransferData, IEnumerable<(Guid AppInstanceId, Guid ClientId, string ClientClientId)> AppInstanceData) result)
    {
        var existingData = result.SetupTransferData;
        var instanceTypeChanged = existingData.IsSingleInstance != data.IsSingleInstance;
        _portalRepositories.GetInstance<IOfferRepository>().AttachAndModifyAppInstanceSetup(
            existingData.Id,
            appId,
            entity =>
            {
                entity.InstanceUrl = data.InstanceUrl;
                entity.IsSingleInstance = data.IsSingleInstance;
            },
            entity =>
            {
                entity.InstanceUrl = existingData.InstanceUrl;
                entity.IsSingleInstance = existingData.IsSingleInstance;
            });

        (Guid AppInstanceId, Guid ClientId, string ClientClientId) appInstance;
        switch (instanceTypeChanged)
        {
            case true when existingData.IsSingleInstance:
                appInstance = GetAndValidateSingleAppInstance(result.AppInstanceData);
                await _offerSetupService
                    .DeleteSingleInstance(appInstance.AppInstanceId, appInstance.ClientId, appInstance.ClientClientId)
                    .ConfigureAwait(false);
                break;

            case true when data.IsSingleInstance:
                await _offerSetupService
                    .SetupSingleInstance(appId, data.InstanceUrl!)
                    .ConfigureAwait(false);
                break;

            case false when data.IsSingleInstance && existingData.InstanceUrl != data.InstanceUrl:
                appInstance = GetAndValidateSingleAppInstance(result.AppInstanceData);
                await _offerSetupService
                    .UpdateSingleInstance(appInstance.ClientClientId, data.InstanceUrl!)
                    .ConfigureAwait(false);
                break;
        }
    }

    private static (Guid AppInstanceId, Guid ClientId, string ClientClientId) GetAndValidateSingleAppInstance(IEnumerable<(Guid AppInstanceId, Guid ClientId, string ClientClientId)> appInstanceData)
    {
        if (appInstanceData.Count() != 1)
        {
            throw new ConflictException("The must be at exactly one AppInstance");
        }

        return appInstanceData.Single();
    }
}
