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

using AutoFixture;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.Eclipse.TractusX.Portal.Backend.Apps.Service.BusinessLogic;
using Org.Eclipse.TractusX.Portal.Backend.Apps.Service.ViewModels;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Models;
using Org.Eclipse.TractusX.Portal.Backend.Offers.Library.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Entities;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;
using Org.Eclipse.TractusX.Portal.Backend.Tests.Shared;
using Org.Eclipse.TractusX.Portal.Backend.Tests.Shared.Extensions;
using Xunit;

namespace Org.Eclipse.TractusX.Portal.Backend.Apps.Service.Controllers.Tests;

public class AppReleaseProcessControllerTest
{
    private static readonly string IamUserId = "4C1A6851-D4E7-4E10-A011-3732CD045E8A";
    private readonly IFixture _fixture;
    private readonly AppReleaseProcessController _controller;
    private readonly IAppReleaseBusinessLogic _logic;

    public AppReleaseProcessControllerTest()
    {
        _fixture = new Fixture();
        _logic = A.Fake<IAppReleaseBusinessLogic>();
        this._controller = new AppReleaseProcessController(_logic);
        _controller.AddControllerContextWithClaim(IamUserId);
    }

    [Fact]
    public async Task UpdateApp_ReturnsNoContent()
    {
        // Arrange
        var appId = new Guid("5cf74ef8-e0b7-4984-a872-474828beb5d2");
        var data = new AppEditableDetail(
            new LocalizedDescription[]
            {
                new("en", "This is a long description", "description")
            },
            "https://test.provider.com",
            null,
            null);
        A.CallTo(() => _logic.UpdateAppAsync(A<Guid>._, A<AppEditableDetail>._, A<string>._))
            .ReturnsLazily(() => Task.CompletedTask);

        // Act
        var result = await this._controller.UpdateApp(appId, data).ConfigureAwait(false);
        
        // Assert
        Assert.IsType<NoContentResult>(result);
        A.CallTo(() => _logic.UpdateAppAsync(appId, data, IamUserId)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAppDocument_ReturnsExpectedResult()
    {
        //Arrange
        Guid appId = new Guid("5cf74ef8-e0b7-4984-a872-474828beb5d2");
        var documentTypeId = DocumentTypeId.ADDITIONAL_DETAILS;
        var file = FormFileHelper.GetFormFile("this is just a test", "superFile.pdf", "application/pdf");

        A.CallTo(() => _logic.CreateAppDocumentAsync(A<Guid>._, A<DocumentTypeId>._, A<FormFile>._, A<string>._, A<CancellationToken>._))
            .ReturnsLazily(() => Task.CompletedTask);

        //Act
        await this._controller.UpdateAppDocumentAsync(appId, documentTypeId, file, CancellationToken.None).ConfigureAwait(false);
        
        // Assert 
        A.CallTo(() => _logic.CreateAppDocumentAsync(appId, documentTypeId, file, IamUserId, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddAppUserRole_AndUserRoleDescriptionWith201StatusCode()
    {
        //Arrange
        Guid appId = new Guid("5cf74ef8-e0b7-4984-a872-474828beb5d2");
        Guid userId = new Guid("7eab8e16-8298-4b41-953b-515745423658");
        var appUserRoles = _fixture.CreateMany<AppUserRole>(3);
        var appRoleData = _fixture.CreateMany<AppRoleData>(3);
        A.CallTo(() => _logic.AddAppUserRoleAsync(appId, appUserRoles,userId.ToString()))
            .Returns(appRoleData);

        //Act
        var result = await this._controller.AddAppUserRole(appId, appUserRoles).ConfigureAwait(false);
        foreach (var item in result)
        {
            //Assert
            A.CallTo(() => _logic.AddAppUserRoleAsync(appId, appUserRoles,userId.ToString())).MustHaveHappenedOnceExactly();
            Assert.NotNull(item);
            Assert.IsType<AppRoleData>(item);
        }
    }

    [Fact]
    public async Task GetOfferAgreementData_ReturnsExpectedResult()
    {
        //Arrange
        var data = _fixture.CreateMany<AgreementDocumentData>(5).ToAsyncEnumerable();
        A.CallTo(() => _logic.GetOfferAgreementDataAsync())
            .Returns(data);

        //Act
        var result = await this._controller.GetOfferAgreementDataAsync().ToListAsync().ConfigureAwait(false);
        
        // Assert 
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetOfferAgreementConsentById_ReturnsExpectedResult()
    {
        //Arrange
        var appId = Guid.NewGuid();
        var data = _fixture.Create<OfferAgreementConsent>();
        A.CallTo(() => _logic.GetOfferAgreementConsentById(A<Guid>._, A<string>._))
            .Returns(data);

        //Act
        var result = await this._controller.GetOfferAgreementConsentById(appId).ConfigureAwait(false);
        
        // Assert 
        result.Should().Be(data);
        A.CallTo(() => _logic.GetOfferAgreementConsentById(appId, IamUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SubmitOfferConsentToAgreementsAsync_ReturnsExpectedResult()
    {
        //Arrange
        var appId = Guid.NewGuid();
        var data = _fixture.Create<OfferAgreementConsent>();
        var consentStatusData = new ConsentStatusData(Guid.NewGuid(), ConsentStatusId.ACTIVE);
        A.CallTo(() => _logic.SubmitOfferConsentAsync(A<Guid>._, A<OfferAgreementConsent>._, A<string>._))
            .ReturnsLazily(() => Enumerable.Repeat(consentStatusData, 1));

        //Act
        var result = await this._controller.SubmitOfferConsentToAgreementsAsync(appId, data).ConfigureAwait(false);
        
        // Assert 
        result.Should().HaveCount(1);
        A.CallTo(() => _logic.SubmitOfferConsentAsync(appId, data, IamUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAppDetailsForStatusAsync_ReturnsExpectedResult()
    {
        //Arrange
        var appId = Guid.NewGuid();
        var data = _fixture.Create<AppProviderResponse>();
        A.CallTo(() => _logic.GetAppDetailsForStatusAsync(A<Guid>._, A<string>._))
            .ReturnsLazily(() => data);

        //Act
        var result = await this._controller.GetAppDetailsForStatusAsync(appId).ConfigureAwait(false);
        
        // Assert 
        result.Should().Be(data);
        A.CallTo(() => _logic.GetAppDetailsForStatusAsync(appId, IamUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteAppRoleAsync_ReturnsExpectedResult()
    {
        //Arrange
        var appId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        A.CallTo(() => _logic.DeleteAppRoleAsync(A<Guid>._, A<Guid>._, A<string>._))
            .ReturnsLazily(()=> Task.CompletedTask);

        //Act
        var result = await this._controller.DeleteAppRoleAsync(appId, roleId).ConfigureAwait(false);
        
        // Assert 
        Assert.IsType<NoContentResult>(result);
        A.CallTo(() => _logic.DeleteAppRoleAsync(appId, roleId, IamUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAppProviderSalesManagerAsync_ReturnsExpectedResult()
    {
        //Arrange
        var data = _fixture.CreateMany<CompanyUserNameData>(5).ToAsyncEnumerable();
        A.CallTo(() => _logic.GetAppProviderSalesManagersAsync(A<string>._))
            .ReturnsLazily(() => data);

        //Act
        var result = await this._controller.GetAppProviderSalesManagerAsync().ToListAsync().ConfigureAwait(false);
        
        // Assert 
        result.Should().HaveCount(5);
        A.CallTo(() => _logic.GetAppProviderSalesManagersAsync(IamUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAppCreation_ReturnsExpectedId()
    {
        //Arrange
        var appId = _fixture.Create<Guid>();
        var data = _fixture.Create<AppRequestModel>();
        A.CallTo(() => _logic.AddAppAsync(A<AppRequestModel>._, A<string>._))
            .ReturnsLazily(() => appId);

        //Act
        var result = await this._controller.ExecuteAppCreation(data).ConfigureAwait(false);

        //Assert
        A.CallTo(() => _logic.AddAppAsync(data, IamUserId)).MustHaveHappenedOnceExactly();
        Assert.IsType<CreatedAtRouteResult>(result);
        result.Value.Should().Be(appId);
    }

    [Fact]
    public async Task UpdateAppRelease_ReturnsNoContent()
    {
        // Arrange
        var appId = new Guid("5cf74ef8-e0b7-4984-a872-474828beb5d2");
        var data = new AppRequestModel(
            "Test",
            "Test Provider",
            Guid.NewGuid(),
            new []
            {
                Guid.NewGuid()
            },
            new LocalizedDescription[]
            {
                new("en", "This is a long description", "description")
            },
            new[]
            {
                "https://test.com/image.jpg"
            },
            "19€",
            new[]
            {
                PrivacyPolicyId.COMPANY_DATA 
            },
            "https://test.provider.com",
            "test@gmail.com",
            "9456321678"
            );
        A.CallTo(() => _logic.UpdateAppReleaseAsync(A<Guid>._, A<AppRequestModel>._, A<string>._))
            .ReturnsLazily(() => Task.CompletedTask);

        // Act
        var result = await this._controller.UpdateAppRelease(appId, data).ConfigureAwait(false);
        
        // Assert
        Assert.IsType<NoContentResult>(result);
        A.CallTo(() => _logic.UpdateAppReleaseAsync(appId, data, IamUserId)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAllInReviewStatusAppsAsync_ReturnsExpectedCount()
    {
        //Arrange
        var paginationResponse = new Pagination.Response<InReviewAppData>(new Pagination.Metadata(15, 1, 1, 15), _fixture.CreateMany<InReviewAppData>(5));
        A.CallTo(() => _logic.GetAllInReviewStatusAppsAsync(A<int>._, A<int>._,A<OfferSorting?>._,null))
            .ReturnsLazily(() => paginationResponse);

        //Act
        var result = await this._controller.GetAllInReviewStatusAppsAsync().ConfigureAwait(false);

        //Assert
        A.CallTo(() => _logic.GetAllInReviewStatusAppsAsync(0, 15, null, null)).MustHaveHappenedOnceExactly();
        result.Content.Should().HaveCount(5);
    }

    [Fact]
    public async Task SubmitAppReleaseRequest_ReturnsExpectedCount()
    {
        //Arrange
        var appId = _fixture.Create<Guid>();
        A.CallTo(() => _logic.SubmitAppReleaseRequestAsync(appId, A<string>._))
            .ReturnsLazily(() => Task.CompletedTask);

        //Act
        var result = await this._controller.SubmitAppReleaseRequest(appId).ConfigureAwait(false);

        //Assert
        A.CallTo(() => _logic.SubmitAppReleaseRequestAsync(appId, IamUserId)).MustHaveHappenedOnceExactly();
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ApproveAppRequest_ReturnsExpectedCount()
    {
        //Arrange
        var appId = _fixture.Create<Guid>();
        A.CallTo(() => _logic.ApproveAppRequestAsync(appId, A<string>._))
            .ReturnsLazily(() => Task.CompletedTask);

        //Act
        var result = await this._controller.ApproveAppRequest(appId).ConfigureAwait(false);

        //Assert
        A.CallTo(() => _logic.ApproveAppRequestAsync(appId, IamUserId)).MustHaveHappenedOnceExactly();
        Assert.IsType<NoContentResult>(result);
    }

     [Fact]
    public async Task DeclineAppRequest_ReturnsNoContent()
    {
        //Arrange
        var appId = _fixture.Create<Guid>();
        var data = new OfferDeclineRequest("Just a test");
        A.CallTo(() => _logic.DeclineAppRequestAsync(A<Guid>._, A<string>._, A<OfferDeclineRequest>._))
            .ReturnsLazily(() => Task.CompletedTask);

        //Act
        var result = await this._controller.DeclineAppRequest(appId, data).ConfigureAwait(false);

        //Assert
        A.CallTo(() => _logic.DeclineAppRequestAsync(appId, IamUserId, data)).MustHaveHappenedOnceExactly();
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetinReviewAppDetailsByIdAsync_ReturnsExpectedResult()
    {
        //Arrange
        var appId = _fixture.Create<Guid>();
        var data = new InReviewAppDetails(appId,"Catena-X",default,null!,null!,null!,null!,null!,null!,null!,null!,null!,null!,LicenseTypeId.COTS,null!,null!,new[]{PrivacyPolicyId.COMPANY_DATA});
        A.CallTo(() => _logic.GetInReviewAppDetailsByIdAsync(appId))
            .ReturnsLazily(() => data);
        
        //Act
        var result = await this._controller.GetInReviewAppDetailsByIdAsync(appId);

        //Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Catena-X");
    }

    [Fact]
    public async Task DeleteAppDocumentsAsync_ReturnsExpectedResult()
    {
        //Arrange
        var documentId = Guid.NewGuid();
        A.CallTo(() => _logic.DeleteAppDocumentsAsync(A<Guid>._, A<string>._))
            .ReturnsLazily(()=> Task.CompletedTask);

        //Act
        var result = await this._controller.DeleteAppDocumentsAsync(documentId).ConfigureAwait(false);
        
        // Assert 
        Assert.IsType<NoContentResult>(result);
        A.CallTo(() => _logic.DeleteAppDocumentsAsync(documentId, IamUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteAppAsync_ReturnsExpectedResult()
    {
        //Arrange
        var appId = _fixture.Create<Guid>();
        A.CallTo(() => _logic.DeleteAppAsync(A<Guid>._, A<string>._))
            .ReturnsLazily(()=> Task.CompletedTask);

        //Act
        var result = await this._controller.DeleteAppAsync(appId).ConfigureAwait(false);
        
        // Assert 
        Assert.IsType<NoContentResult>(result);
        A.CallTo(() => _logic.DeleteAppAsync(appId, IamUserId))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SetInstanceType_ReturnsExpectedResult()
    {
        var appId = _fixture.Create<Guid>();
        var data = new AppInstanceSetupData(true, "https://test.de");

        var result = await _controller.SetInstanceType(appId, data).ConfigureAwait(false);

        Assert.IsType<NoContentResult>(result);
        A.CallTo(() => _logic.SetInstanceType(appId, data, IamUserId))
            .MustHaveHappenedOnceExactly();
    }
}
