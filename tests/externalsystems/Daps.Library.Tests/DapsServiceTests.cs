﻿/********************************************************************************
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

using System.Net;
using Microsoft.Extensions.Options;
using Org.Eclipse.TractusX.Portal.Backend.Framework.ErrorHandling;
using Org.Eclipse.TractusX.Portal.Backend.Framework.Token;
using Org.Eclipse.TractusX.Portal.Backend.Tests.Shared;

namespace Org.Eclipse.TractusX.Portal.Backend.Daps.Library.Tests;

public class DapsServiceTests
{
    #region Initialization
    
    private readonly ITokenService _tokenService;
    private readonly IOptions<DapsSettings> _options;

    public DapsServiceTests()
    {
        var fixture = new Fixture().Customize(new AutoFakeItEasyCustomization { ConfigureMembers = true });
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _options = Options.Create(new DapsSettings
        {
            DapsUrl = "https://www.api.daps-not-existing.com",
            Password = "passWord",
            Scope = "test",
            Username = "user@name",
            ClientId = "CatenaX",
            ClientSecret = "pass@Secret",
            GrantType = "cred",
            KeycloakTokenAddress = "https://key.cloak.com",
        });
        _tokenService = A.Fake<ITokenService>();
    }

    #endregion
    
    #region EnableDapsAuth
    
    [Fact]
    public async Task EnableDapsAuthAsync_WithValidCall_ReturnsExpected()
    {
        // Arrange
        var file = FormFileHelper.GetFormFile("Content of the super secure certificate", "test.pem", "application/x-pem-file");

        var content = "{\"daps\": {\"jwks\": \"https://daps-pen.int.demo.catena-x.net/jwks.json\"},\"clientId\": \"11:11:11:11:11:ED:E2:EF:EF:72:14:BA:87:95:CF:C1:AC:B0:84:E5:keyid:1C:AA:6E:30:30:ED:E2:EF:EF:72:14:BA:87:95:CF:11:11:11:11:11\"}";
        var httpMessageHandlerMock = new HttpMessageHandlerMock(HttpStatusCode.Created, new StringContent(content));
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        const string clientName = "Connec Tor";
        const string referringConnector = "https://connect-tor.com";
        const string businessPartnerNumber = "BPNL000000000009";
        var service = new DapsService(_tokenService, _options);

        // Act
        var result = await service.EnableDapsAuthAsync(clientName, referringConnector, businessPartnerNumber, file, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().NotBeNull();
        result!.ClientId.Should().Be("11:11:11:11:11:ED:E2:EF:EF:72:14:BA:87:95:CF:C1:AC:B0:84:E5:keyid:1C:AA:6E:30:30:ED:E2:EF:EF:72:14:BA:87:95:CF:11:11:11:11:11");
    }

    [Fact]
    public async Task EnableDapsAuthAsync_WithUnsuccessfulStatusCode_ThrowsException()
    {
        // Arrange
        var file = FormFileHelper.GetFormFile("Content of the super secure certificate", "test.pem", "application/x-pem-file");

        var httpMessageHandlerMock =
            new HttpMessageHandlerMock(HttpStatusCode.BadRequest);
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        const string clientName = "Connec Tor";
        const string referringConnector = "https://connect-tor.com";
        const string businessPartnerNumber = "BPNL000000000009";
        var service = new DapsService(_tokenService, _options);

        // Act
        async Task Act() => await service.EnableDapsAuthAsync(clientName, referringConnector, businessPartnerNumber, file, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.ThrowsAsync<ServiceException>(Act).ConfigureAwait(false);
    }

    [Fact]
    public async Task EnableDapsAuthAsync_WithException_ThrowsException()
    {
        // Arrange
        var file = FormFileHelper.GetFormFile("Content of the super secure certificate", "test.pem", "application/x-pem-file");

        var httpMessageHandlerMock =
            new HttpMessageHandlerMock(HttpStatusCode.BadRequest, ex:  new HttpRequestException ("DNS Error"));
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        const string clientName = "Connec Tor";
        const string referringConnector = "https://connect-tor.com";
        const string businessPartnerNumber = "BPNL000000000009";
        var service = new DapsService(_tokenService, _options);

        // Act
        async Task Act() => await service.EnableDapsAuthAsync(clientName, referringConnector, businessPartnerNumber, file, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.ThrowsAsync<ServiceException>(Act).ConfigureAwait(false);
    }

    [Fact]
    public async Task EnableDapsAuthAsync_WithInvalidUrl_ThrowsException()
    {
        // Arrange
        var file = FormFileHelper.GetFormFile("Content of the super secure certificate", "test.pem", "application/x-pem-file");

        var httpMessageHandlerMock =
            new HttpMessageHandlerMock(HttpStatusCode.BadRequest, ex:  new HttpRequestException ("DNS Error"));
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        const string clientName = "Connec Tor";
        const string referringConnector = "test.com";
        const string businessPartnerNumber = "BPNL000000000009";
        var service = new DapsService(_tokenService, _options);

        // Act
        async Task Act() => await service.EnableDapsAuthAsync(clientName, referringConnector, businessPartnerNumber, file, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.ThrowsAsync<ControllerArgumentException>(Act).ConfigureAwait(false);
    }

    #endregion
    
    #region DeleteDapsAuth
    
    [Fact]
    public async Task DeleteDapsAuth_WithValidCall_ReturnsExpected()
    {
        // Arrange
        var httpMessageHandlerMock = new HttpMessageHandlerMock(HttpStatusCode.OK);
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        var service = new DapsService(_tokenService, _options);

        // Act
        var result = await service.DeleteDapsClient("1234", CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDapsClient_WithUnsuccessfulStatusCode_ThrowsException()
    {
        // Arrange
        var httpMessageHandlerMock = new HttpMessageHandlerMock(HttpStatusCode.BadRequest);
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        var service = new DapsService(_tokenService, _options);

        // Act
        async Task Act() => await service.DeleteDapsClient("1234", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.ThrowsAsync<ServiceException>(Act).ConfigureAwait(false);
    }

    [Fact]
    public async Task DeleteDapsClient_WithException_ThrowsException()
    {
        // Arrange
        var httpMessageHandlerMock = new HttpMessageHandlerMock(HttpStatusCode.BadRequest, ex:  new HttpRequestException ("DNS Error"));
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        var service = new DapsService(_tokenService, _options);

        // Act
        async Task Act() => await service.DeleteDapsClient("12345", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.ThrowsAsync<ServiceException>(Act).ConfigureAwait(false);
    }

    #endregion
    
    #region UpdateDapsConnectorUrl
    
    [Fact]
    public async Task UpdateDapsConnectorUrl_WithValidCall_ReturnsExpected()
    {
        // Arrange
        var httpMessageHandlerMock = new HttpMessageHandlerMock(HttpStatusCode.OK);
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        var service = new DapsService(_tokenService, _options);

        // Act
        var result = await service.UpdateDapsConnectorUrl("1234", "https://test.url.com", "BPNL123456789", CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateDapsConnectorUrl_WithUnsuccessfulStatusCode_ThrowsException()
    {
        // Arrange
        var httpMessageHandlerMock = new HttpMessageHandlerMock(HttpStatusCode.BadRequest);
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        var service = new DapsService(_tokenService, _options);

        // Act
        async Task Act() => await service.UpdateDapsConnectorUrl("1234", "https://test.url.com", "BPNL123456789", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.ThrowsAsync<ServiceException>(Act).ConfigureAwait(false);
    }

    [Fact]
    public async Task UpdateDapsConnectorUrl_WithException_ThrowsException()
    {
        // Arrange
        var httpMessageHandlerMock = new HttpMessageHandlerMock(HttpStatusCode.BadRequest, ex:  new HttpRequestException ("DNS Error"));
        var httpClient = new HttpClient(httpMessageHandlerMock)
        {
            BaseAddress = new Uri("https://base.address.com")
        };
        A.CallTo(() => _tokenService.GetAuthorizedClient<DapsService>(_options.Value, A<CancellationToken>._))
            .Returns(httpClient);
        var service = new DapsService(_tokenService, _options);

        // Act
        async Task Act() => await service.UpdateDapsConnectorUrl("12345", "https://test.url.com", "BPNL123456789", CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.ThrowsAsync<ServiceException>(Act).ConfigureAwait(false);
    }

    #endregion
}
