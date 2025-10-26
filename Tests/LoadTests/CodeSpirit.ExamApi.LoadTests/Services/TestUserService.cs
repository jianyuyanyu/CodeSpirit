using CodeSpirit.ExamApi.LoadTests.Models;
using Newtonsoft.Json;

namespace CodeSpirit.ExamApi.LoadTests.Services;

/// <summary>
/// 测试用户服务
/// </summary>
public class TestUserService
{
    private readonly List<TestUser> _testUsers;
    private readonly Random _random;
    private int _currentIndex = 0;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TestUserService()
    {
        _testUsers = new List<TestUser>();
        _random = new Random();
    }

    /// <summary>
    /// 从文件加载测试用户
    /// </summary>
    /// <param name="filePath">用户文件路径</param>
    /// <returns>加载的用户数量</returns>
    public async Task<int> LoadUsersFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"测试用户文件不存在: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        var usernames = JsonConvert.DeserializeObject<List<string>>(json);
        
        if (usernames != null)
        {
            _testUsers.Clear();
            foreach (var username in usernames)
            {
                _testUsers.Add(TestUser.FromUsername(username));
            }
        }

        return _testUsers.Count;
    }

    /// <summary>
    /// 获取随机测试用户
    /// </summary>
    /// <returns>随机测试用户</returns>
    public TestUser GetRandomUser()
    {
        if (!_testUsers.Any())
        {
            throw new InvalidOperationException("没有可用的测试用户，请先加载用户数据");
        }

        var index = _random.Next(_testUsers.Count);
        return _testUsers[index];
    }

    /// <summary>
    /// 按顺序获取测试用户（轮询方式）
    /// </summary>
    /// <returns>测试用户</returns>
    public TestUser GetNextUser()
    {
        if (!_testUsers.Any())
        {
            throw new InvalidOperationException("没有可用的测试用户，请先加载用户数据");
        }

        var user = _testUsers[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _testUsers.Count;
        return user;
    }

    /// <summary>
    /// 获取指定索引的用户
    /// </summary>
    /// <param name="index">用户索引</param>
    /// <returns>测试用户</returns>
    public TestUser GetUserByIndex(int index)
    {
        if (!_testUsers.Any())
        {
            throw new InvalidOperationException("没有可用的测试用户，请先加载用户数据");
        }

        if (index < 0 || index >= _testUsers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"用户索引超出范围: {index}");
        }

        return _testUsers[index];
    }

    /// <summary>
    /// 获取用户总数
    /// </summary>
    /// <returns>用户总数</returns>
    public int GetUserCount()
    {
        return _testUsers.Count;
    }

    /// <summary>
    /// 获取所有用户
    /// </summary>
    /// <returns>所有测试用户</returns>
    public List<TestUser> GetAllUsers()
    {
        return _testUsers.ToList();
    }

    /// <summary>
    /// 清除所有用户的Token缓存
    /// </summary>
    public void ClearTokenCache()
    {
        foreach (var user in _testUsers)
        {
            user.AuthToken = null;
            user.TokenExpiry = null;
        }
    }
}
