namespace CodeSpirit.Core.IdGenerator
{
    /// <summary>
    /// ID生成器接口
    /// </summary>
    public interface IIdGenerator
    {
        /// <summary>
        /// 生成新的ID
        /// </summary>
        /// <returns></returns>
        long NewId();
    }
}