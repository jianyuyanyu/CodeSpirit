namespace CodeSpirit.Shared.Entities
{
    public class DefaultWithoutAuditedEntity<TKey> : Entity<TKey>, ITenant
    {
        /// <summary>
        ///     租户Id
        /// </summary>
        public int? TenantId { get; set; }
    }
}
