namespace CodeSpirit.Amis.Attributes;

/// <summary>
/// 标记方法不应被识别为CRUD操作的特性。
/// 用于排除某些方法被自动识别为Create、Read、Update、Delete、Import、Export等标准操作。
/// </summary>
/// <remarks>
/// 当方法名符合CRUD操作的命名约定（如以Import、Export、Create等开头），
/// 但实际上是特定的业务操作而非标准CRUD操作时，可以使用此特性进行排除。
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class IgnoreCrudAttribute : Attribute
{
}
