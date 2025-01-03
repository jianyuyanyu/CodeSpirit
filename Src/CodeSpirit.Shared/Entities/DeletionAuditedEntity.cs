namespace CodeSpirit.Shared.Entities
{
    /// <summary>
    /// 数据模型(仅带软删)
    /// </summary>
    /// <typeparam name="TKey"></typeparam>

    public abstract class DeletionAuditedEntity<TKey> : Entity<TKey>, IDeletionAuditedObject
    {

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

    }
}
