namespace CodeSpirit.Shared.Entities
{
    public interface IDeletionAuditedObject
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
