﻿using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tudou.Abp.Identity.Localization;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace Tudou.Abp.Identity.OrganizationUnits
{
    public class OrganizationUnitManager : DomainService
    {
        protected IOrganizationUnitRepository _organizationUnitRepository { get; private set; }

        private readonly IStringLocalizer<IdentityResource> _localizer;
        private readonly IIdentityRoleRepository _identityRoleRepository;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        public OrganizationUnitManager(
          IOrganizationUnitRepository organizationUnitRepository,
          IStringLocalizer<IdentityResource> localizer,
          IIdentityRoleRepository identityRoleRepository,
          ICancellationTokenProvider cancellationTokenProvider)
        {
            _organizationUnitRepository = organizationUnitRepository;
            _localizer = localizer;
            _identityRoleRepository = identityRoleRepository;
            _cancellationTokenProvider = cancellationTokenProvider;
        }
        [UnitOfWork]
        public virtual async Task CreateAsync(OrganizationUnit organizationUnit)
        {
            organizationUnit.Code = await GetNextChildCodeAsync(organizationUnit.ParentId);
            await ValidateOrganizationUnitAsync(organizationUnit);
            await _organizationUnitRepository.InsertAsync(organizationUnit).ConfigureAwait(false);
        }
        public virtual async Task UpdateAsync(OrganizationUnit organizationUnit)
        {
            await ValidateOrganizationUnitAsync(organizationUnit);
            await _organizationUnitRepository.UpdateAsync(organizationUnit).ConfigureAwait(false);
        }

        public virtual async Task<string> GetNextChildCodeAsync(Guid? parentId)
        {
            var lastChild = await GetLastChildOrNullAsync(parentId);
            if (lastChild == null)
            {
                var parentCode = parentId != null ? await GetCodeOrDefaultAsync(parentId.Value) : null;
                return OrganizationUnit.AppendCode(parentCode, OrganizationUnit.CreateCode(1));
            }

            return OrganizationUnit.CalculateNextCode(lastChild.Code);
        }

        public virtual async Task<OrganizationUnit> GetLastChildOrNullAsync(Guid? parentId)
        {
            var children = await _organizationUnitRepository.GetChildrenAsync(parentId);
            return children.OrderBy(c => c.Code).LastOrDefault();
        }

        [UnitOfWork]
        public virtual async Task DeleteAsync(Guid id)
        {
            var children = await FindChildrenAsync(id, true);

            foreach (var child in children)
            {
                await _organizationUnitRepository.DeleteAsync(child).ConfigureAwait(false);
            }

            await _organizationUnitRepository.DeleteAsync(id).ConfigureAwait(false);
        }

        [UnitOfWork]
        public virtual async Task MoveAsync(Guid id, Guid? parentId)
        {
            var organizationUnit = await _organizationUnitRepository.GetAsync(id).ConfigureAwait(false);
            if (organizationUnit.ParentId == parentId)
            {
                return;
            }

            //Should find children before Code change
            var children = await FindChildrenAsync(id, true);

            //Store old code of OU
            var oldCode = organizationUnit.Code;

            //Move OU
            organizationUnit.Code = await GetNextChildCodeAsync(parentId);
            organizationUnit.ParentId = parentId;

            await ValidateOrganizationUnitAsync(organizationUnit);

            //Update Children Codes
            foreach (var child in children)
            {
                child.Code = OrganizationUnit.AppendCode(organizationUnit.Code, OrganizationUnit.GetRelativeCode(child.Code, oldCode));
            }
        }

        public virtual async Task<string> GetCodeOrDefaultAsync(Guid id)
        {
            var ou = await _organizationUnitRepository.GetAsync(id).ConfigureAwait(false);
            return ou?.Code;
        }

        protected virtual async Task ValidateOrganizationUnitAsync(OrganizationUnit organizationUnit)
        {
            var siblings = (await FindChildrenAsync(organizationUnit.ParentId))
                .Where(ou => ou.Id != organizationUnit.Id)
                .ToList();

            if (siblings.Any(ou => ou.DisplayName == organizationUnit.DisplayName))
            {
                throw new UserFriendlyException(_localizer["OrganizationUnitDuplicateDisplayNameWarning", organizationUnit.DisplayName]);
            }
        }

        public async Task<List<OrganizationUnit>> FindChildrenAsync(Guid? parentId, bool recursive = false)
        {
            if (!recursive)
            {
                return await _organizationUnitRepository.GetChildrenAsync(parentId).ConfigureAwait(false);
            }

            if (!parentId.HasValue)
            {
                return await _organizationUnitRepository.GetListAsync().ConfigureAwait(false);
            }

            var code = await GetCodeOrDefaultAsync(parentId.Value);

            return await _organizationUnitRepository.GetAllChildrenWithParentCodeAsync(code, parentId);
        }

        public virtual Task<bool> IsInOrganizationUnitAsync(IdentityUser user, OrganizationUnit ou)
        {
            return Task.FromResult(user.IsInOrganizationUnit(ou.Id));
        }


    }
}
