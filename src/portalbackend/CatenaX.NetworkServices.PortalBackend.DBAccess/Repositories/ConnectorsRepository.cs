﻿using CatenaX.NetworkServices.PortalBackend.PortalEntities;
using CatenaX.NetworkServices.PortalBackend.PortalEntities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CatenaX.NetworkServices.Framework.ErrorHandling;

namespace CatenaX.NetworkServices.PortalBackend.DBAccess.Repositories;

/// Implementation of <see cref="IConnectorsRepository"/> accessing database with EF Core.
public class ConnectorsRepository : IConnectorsRepository
{
    private readonly PortalDbContext _context;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="portalDbContext">PortalDb context.</param>
    public ConnectorsRepository(PortalDbContext portalDbContext)
    {
        this._context = portalDbContext;
    }

    /// <inheritdoc/>
    public IQueryable<Connector> GetAllCompanyConnectorsForIamUser(string iamUserId)
    {
        return _context.IamUsers.AsNoTracking()
            .Where(u => u.UserEntityId == iamUserId)
            .SelectMany(u => u.CompanyUser!.Company!.ProvidedConnectors);
    }

    /// <inheritdoc/>
    public Connector CreateConnectorAsync(string name, string location, string connectorUrl, Action<Connector>? setupOptionalFields)
    {
        var connector = new Connector(Guid.NewGuid(), name, location, connectorUrl);
        setupOptionalFields?.Invoke(connector);
        return _context.Connectors.Add(connector).Entity;
    }

    /// <inheritdoc/>
    public async Task DeleteConnectorAsync(Guid connectorId)
    {
        try
        {
            var connector = new Connector(connectorId, string.Empty, string.Empty, string.Empty);
            _context.Connectors.Attach(connector);
            _context.Connectors.Remove(connector);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new NotFoundException("Connector with provided ID does not exist.");
        }
    }
}
