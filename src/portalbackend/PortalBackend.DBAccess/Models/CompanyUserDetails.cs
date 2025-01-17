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

using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;
using System.Text.Json.Serialization;

namespace Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;

public record CompanyUserDetails(
    [property: JsonPropertyName("companyUserId")] Guid companyUserId,
    [property: JsonPropertyName("created")] DateTimeOffset createdAt,
    [property: JsonPropertyName("bpn")] IEnumerable<string> businessPartnerNumbers,
    [property: JsonPropertyName("company")] string companyName,
    [property: JsonPropertyName("status")] CompanyUserStatusId companyUserStatusId,
    [property: JsonPropertyName("assignedRoles")] IEnumerable<CompanyUserAssignedRoleDetails> assignedRoles)
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public record CompanyUserAssignedRoleDetails(
    [property: JsonPropertyName("appId")] Guid OfferId,
    [property: JsonPropertyName("roles")] IEnumerable<string> UserRoles);
