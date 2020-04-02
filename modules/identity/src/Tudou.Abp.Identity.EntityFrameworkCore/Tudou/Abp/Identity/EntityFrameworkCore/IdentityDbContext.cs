﻿using Microsoft.EntityFrameworkCore;
using Tudou.Abp.Identity.OrganizationUnits;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace Tudou.Abp.Identity.EntityFrameworkCore
{
    /// <summary>
    /// Base class for the Entity Framework database context used for identity.
    /// </summary>
    [ConnectionStringName(AbpIdentityDbProperties.ConnectionStringName)]
    public class IdentityDbContext : AbpDbContext<IdentityDbContext>, IIdentityDbContext
    {
        public DbSet<IdentityUser> Users { get; set; }

        public DbSet<IdentityUserOrganizationUnit> IdentityUserOrganizationUnits { get; set; }

        public DbSet<IdentityRole> Roles { get; set; }

        public DbSet<IdentityClaimType> ClaimTypes { get; set; }

        public DbSet<OrganizationUnit> OrganizationUnits { get; set; }

        public DbSet<OrganizationUnitRole> OrganizationUnitRoles { get; set; }

        public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ConfigureIdentity();
        }
    }
}