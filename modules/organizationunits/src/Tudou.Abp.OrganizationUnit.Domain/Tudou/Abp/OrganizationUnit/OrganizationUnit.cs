﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Tudou.Abp.OrganizationUnit
{

    public class OrganizationUnit : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        /// <summary>
        /// TenantId of this entity.
        /// </summary>
        public virtual Guid? TenantId { get; set; }

        public virtual OrganizationUnit Parent { get; set; }

        /// <summary>
        /// Parent <see cref="OrganizationUnit"/> Id.
        /// Null, if this OU is root.
        /// </summary>
        public virtual Guid? ParentId { get; set; }

        public virtual string Code { get; set; }

        public virtual string Name { get; set; }
        /// <summary>
        /// Children of this OU.
        /// </summary>
        public virtual ICollection<OrganizationUnit> Children { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationUnit"/> class.
        /// </summary>
        public OrganizationUnit()
        {
            ExtraProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationUnit"/> class.
        /// </summary>
        /// <param name="tenantId">Tenant's Id or null for host.</param>
        /// <param name="displayName">Display name.</param>
        /// <param name="parentId">Parent's Id or null if OU is a root.</param>
        public OrganizationUnit(Guid id, Guid? tenantId, string name, Guid? parentId = null)
        {
            Id = id;
            TenantId = tenantId;
            Name = name;
            ParentId = parentId;
            ExtraProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates code for given numbers.
        /// Example: if numbers are 4,2 then returns "00004.00002";
        /// </summary>
        /// <param name="numbers">Numbers</param>
        public static string CreateCode(params int[] numbers)
        {
            if (numbers.IsNullOrEmpty())
            {
                return null;
            }

            return numbers.Select(number => number.ToString(new string('0', OrganizationUnitConsts.CodeUnitLength))).JoinAsString(".");
        }

        /// <summary>
        /// Appends a child code to a parent code. 
        /// Example: if parentCode = "00001", childCode = "00042" then returns "00001.00042".
        /// </summary>
        /// <param name="parentCode">Parent code. Can be null or empty if parent is a root.</param>
        /// <param name="childCode">Child code.</param>
        public static string AppendCode(string parentCode, string childCode)
        {
            if (childCode.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(childCode), "childCode can not be null or empty.");
            }

            if (parentCode.IsNullOrEmpty())
            {
                return childCode;
            }

            return parentCode + "." + childCode;
        }

        /// <summary>
        /// Gets relative code to the parent.
        /// Example: if code = "00019.00055.00001" and parentCode = "00019" then returns "00055.00001".
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="parentCode">The parent code.</param>
        public static string GetRelativeCode(string code, string parentCode)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code can not be null or empty.");
            }

            if (parentCode.IsNullOrEmpty())
            {
                return code;
            }

            if (code.Length == parentCode.Length)
            {
                return null;
            }

            return code.Substring(parentCode.Length + 1);
        }

        /// <summary>
        /// Calculates next code for given code.
        /// Example: if code = "00019.00055.00001" returns "00019.00055.00002".
        /// </summary>
        /// <param name="code">The code.</param>
        public static string CalculateNextCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code can not be null or empty.");
            }

            var parentCode = GetParentCode(code);
            var lastUnitCode = GetLastUnitCode(code);

            return AppendCode(parentCode, CreateCode(Convert.ToInt32(lastUnitCode) + 1));
        }

        /// <summary>
        /// Gets the last unit code.
        /// Example: if code = "00019.00055.00001" returns "00001".
        /// </summary>
        /// <param name="code">The code.</param>
        public static string GetLastUnitCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code can not be null or empty.");
            }

            var splittedCode = code.Split('.');
            return splittedCode[splittedCode.Length - 1];
        }

        /// <summary>
        /// Gets parent code.
        /// Example: if code = "00019.00055.00001" returns "00019.00055".
        /// </summary>
        /// <param name="code">The code.</param>
        public static string GetParentCode(string code)
        {
            if (code.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(code), "code can not be null or empty.");
            }

            var splittedCode = code.Split('.');
            if (splittedCode.Length == 1)
            {
                return null;
            }

            return splittedCode.Take(splittedCode.Length - 1).JoinAsString(".");
        }
    }
}
