namespace CodeSpirit.Shared.Entities.Interfaces
{
    public interface IDeletionAuditable
    {
        /// <summary>
        /// 获取或设置删除人ID
        /// </summary>
        long? DeletedBy { get; set; }

        /// <summary>
        /// 获取或设置删除时间
        /// </summary>
        DateTime? DeletedAt { get; set; }
    }
}