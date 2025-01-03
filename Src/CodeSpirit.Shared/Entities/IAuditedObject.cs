namespace CodeSpirit.Shared.Entities
{
    public interface IAuditedObject
    {
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
