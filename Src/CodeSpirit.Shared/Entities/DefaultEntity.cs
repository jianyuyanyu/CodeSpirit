namespace CodeSpirit.Shared.Entities
{
    /// <summary>
    /// 默认数据模型
    /// </summary>
    /// <typeparam name="TKey"></typeparam>

    public abstract class DefaultEntity<TKey> : Entity<TKey>, ITenant, IFullAuditedObject
    {
        /// <summary>
        ///     租户Id
        /// </summary>
        public int? TenantId { get; set; }
        /// <summary>
        ///     是否删除
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        ///     删除时间
        /// </summary>
        public DateTime? DeletionTime { get; set; }

        /// <summary>
        ///     删除者UserId
        /// </summary>
        public long? DeleterUserId { get; set; }

        /// <summary>
        ///     创建者UserId
        /// </summary>
        public long? CreatorUserId { get; set; }

        /// <summary>
        ///     创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        ///     最后修改者UserId
        /// </summary>
        public long? LastModifierUserId { get; set; }

        /// <summary>
        ///     最后修改时间
        /// </summary>
        public DateTime? LastModificationTime { get; set; }
    }
}
