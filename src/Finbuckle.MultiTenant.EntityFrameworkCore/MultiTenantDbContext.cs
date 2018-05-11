using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Finbuckle.MultiTenant.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Finbuckle.MultiTenant.EntityFrameworkCore
{
    /// <summary>
    /// A database context that enforces tenant integrity on entity types
    /// marked with the <c>MultiTenant</c> attribute.
    /// </summary>
    public class MultiTenantDbContext : DbContext
    {
        private readonly TenantContext _tenantContext;
        private ImmutableList<IEntityType> _multiTenantEntityTypes = null;

        protected string ConnectionString => _tenantContext.ConnectionString;

        public MultiTenantDbContext(TenantContext tenantContext, DbContextOptions options) : base(options)
        {
            _tenantContext = tenantContext;
        }

        public MultiTenantDbContext(string connectionString, DbContextOptions options) : base(options)
        {
            _tenantContext = new TenantContext(null, null, null, connectionString, null, null);
        }

        public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;

        public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

        public IImmutableList<IEntityType> MultiTenantEntityTypes
        {
            get
            {
                if (_multiTenantEntityTypes == null)
                {
                    _multiTenantEntityTypes = Model.GetEntityTypes().
                       Where(t => t.ClrType.GetCustomAttribute<MultiTenantAttribute>() != null).
                       ToImmutableList();
                }

                return _multiTenantEntityTypes;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Shared.SetupModel(modelBuilder, _tenantContext);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            if (ChangeTracker.AutoDetectChangesEnabled)
                ChangeTracker.DetectChanges();

            Shared.EnforceTenantId(_tenantContext, ChangeTracker, TenantNotSetMode, TenantMismatchMode);

            var origAutoDetectChange = ChangeTracker.AutoDetectChangesEnabled;
            ChangeTracker.AutoDetectChangesEnabled = false;

            var result = base.SaveChanges(acceptAllChangesOnSuccess);

            ChangeTracker.AutoDetectChangesEnabled = origAutoDetectChange;

            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (ChangeTracker.AutoDetectChangesEnabled)
                ChangeTracker.DetectChanges();

            Shared.EnforceTenantId(_tenantContext, ChangeTracker, TenantNotSetMode, TenantMismatchMode);

            var origAutoDetectChange = ChangeTracker.AutoDetectChangesEnabled;
            ChangeTracker.AutoDetectChangesEnabled = false;

            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);

            ChangeTracker.AutoDetectChangesEnabled = origAutoDetectChange;

            return result;
        }
    }
}