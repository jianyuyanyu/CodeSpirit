namespace CodeSpirit.Shared.DependencyInjection
{
    /// <summary>
    /// 作用域注入
    /// 在同一个作用域中构造的是同一个实例,同一个作用域不同的线程也构造的是同一个实例.
    /// 只有在不同的作用域中构造的是不同的实例.(即同一个请求,都是同一个实例.)
    /// </summary>
    public interface IScopedDependency
    {
    }
}
