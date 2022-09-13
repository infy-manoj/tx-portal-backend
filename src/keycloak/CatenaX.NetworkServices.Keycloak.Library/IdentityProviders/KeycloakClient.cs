/********************************************************************************
 * Copyright (c) 2021,2022 Contributors to https://github.com/lvermeulen/Keycloak.Net.git and BMW Group AG
 * Copyright (c) 2021,2022 Contributors to the CatenaX (ng) GitHub Organisation.
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

using CatenaX.NetworkServices.Keycloak.Library.Models.Common;
using CatenaX.NetworkServices.Keycloak.Library.Models.IdentityProviders;
using Flurl.Http;

namespace CatenaX.NetworkServices.Keycloak.Library;

public partial class KeycloakClient
{
    public async Task<IDictionary<string, object>> ImportIdentityProviderAsync(string realm, string input) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/import-config")
            .PostMultipartAsync(content => content.AddFile(Path.GetFileName(input), Path.GetDirectoryName(input)))
            .ReceiveJson<IDictionary<string, object>>()
            .ConfigureAwait(false);

    public async Task<IDictionary<string, object>> ImportIdentityProviderFromUrlAsync(string realm, string url) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/import-config")
            .PostJsonAsync(new Dictionary<string,string> {
                ["fromUrl"] = url,
                ["providerId"] = "oidc"
            })
            .ReceiveJson<IDictionary<string, object>>()
            .ConfigureAwait(false);

    public async Task CreateIdentityProviderAsync(string realm, IdentityProvider identityProvider) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances")
            .PostJsonAsync(identityProvider)
            .ConfigureAwait(false);

    public async Task<IEnumerable<IdentityProvider>> GetIdentityProviderInstancesAsync(string realm) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances")
            .GetJsonAsync<IEnumerable<IdentityProvider>>()
            .ConfigureAwait(false);

    public async Task<IdentityProvider> GetIdentityProviderAsync(string realm, string identityProviderAlias) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .GetJsonAsync<IdentityProvider>()
            .ConfigureAwait(false);

    /// <summary>
    /// <see cref="https://github.com/keycloak/keycloak-documentation/blob/master/server_development/topics/identity-brokering/tokens.adoc"/>
    /// </summary>
    /// <param name="realm"></param>
    /// <param name="identityProviderAlias"></param>
    /// <returns></returns>
    public async Task<IdentityProviderToken> GetIdentityProviderTokenAsync(string realm, string identityProviderAlias) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/broker/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/token")
            .GetJsonAsync<IdentityProviderToken>()
            .ConfigureAwait(false);

    public async Task UpdateIdentityProviderAsync(string realm, string identityProviderAlias, IdentityProvider identityProvider) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .PutJsonAsync(identityProvider)
            .ConfigureAwait(false);

    public async Task DeleteIdentityProviderAsync(string realm, string identityProviderAlias) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .DeleteAsync()
            .ConfigureAwait(false);

    public async Task ExportIdentityProviderPublicBrokerConfigurationAsync(string realm, string identityProviderAlias) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/export")
            .GetAsync()
            .ConfigureAwait(false);
    
    public async Task<ManagementPermission> GetIdentityProviderAuthorizationPermissionsInitializedAsync(string realm, string identityProviderAlias) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/management/permissions")
            .GetJsonAsync<ManagementPermission>()
            .ConfigureAwait(false);

    public async Task<ManagementPermission> SetIdentityProviderAuthorizationPermissionsInitializedAsync(string realm, string identityProviderAlias, ManagementPermission managementPermission) => 
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/management/permissions")
            .PutJsonAsync(managementPermission)
            .ReceiveJson<ManagementPermission>()
            .ConfigureAwait(false);
    
    public async Task<IDictionary<string, object>> GetIdentityProviderMapperTypesAsync(string realm, string identityProviderAlias) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/mapper-types")
            .GetJsonAsync<IDictionary<string, object>>()
            .ConfigureAwait(false);

    public async Task AddIdentityProviderMapperAsync(string realm, string identityProviderAlias, IdentityProviderMapper identityProviderMapper) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/mappers")
            .PostJsonAsync(identityProviderMapper)
            .ConfigureAwait(false);
    
    public async Task<IEnumerable<IdentityProviderMapper>> GetIdentityProviderMappersAsync(string realm, string identityProviderAlias) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/mappers")
            .GetJsonAsync<IEnumerable<IdentityProviderMapper>>()
            .ConfigureAwait(false);
    
    public async Task<IdentityProviderMapper> GetIdentityProviderMapperByIdAsync(string realm, string identityProviderAlias, string mapperId) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/mappers/")
            .AppendPathSegment(mapperId, true)
            .GetJsonAsync<IdentityProviderMapper>()
            .ConfigureAwait(false);

    public async Task UpdateIdentityProviderMapperAsync(string realm, string identityProviderAlias, string mapperId, IdentityProviderMapper identityProviderMapper) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/mappers/")
            .AppendPathSegment(mapperId, true)
            .PutJsonAsync(identityProviderMapper)
            .ConfigureAwait(false);

    public async Task DeleteIdentityProviderMapperAsync(string realm, string identityProviderAlias, string mapperId) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/instances/")
            .AppendPathSegment(identityProviderAlias, true)
            .AppendPathSegment("/mappers/")
            .AppendPathSegment(mapperId, true)
            .DeleteAsync()
            .ConfigureAwait(false);

    public async Task<IdentityProviderInfo> GetIdentityProviderByProviderIdAsync(string realm, string providerId) =>
        await (await GetBaseUrlAsync(realm).ConfigureAwait(false))
            .AppendPathSegment("/admin/realms/")
            .AppendPathSegment(realm, true)
            .AppendPathSegment("/identity-provider/providers/")
            .AppendPathSegment(providerId, true)
            .GetJsonAsync<IdentityProviderInfo>()
            .ConfigureAwait(false);
}
