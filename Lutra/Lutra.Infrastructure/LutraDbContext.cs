using Lutra.Application.Interfaces;
using Lutra.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lutra.Infrastructure.Sql;

public class LutraDbContext : ILutraDbContext
{
    public DbSet<Supermarkt> Supermarkten => throw new NotImplementedException();

    public DbSet<Verspakket> Verspaketten => throw new NotImplementedException();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
