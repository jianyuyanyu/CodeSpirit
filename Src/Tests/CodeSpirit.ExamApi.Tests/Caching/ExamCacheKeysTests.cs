using CodeSpirit.Caching.Models;
using CodeSpirit.ExamApi.Caching;
using FluentAssertions;
using Xunit;

namespace CodeSpirit.ExamApi.Tests.Caching;

/// <summary>
/// 考试缓存键测试
/// </summary>
public class ExamCacheKeysTests
{
    #region BasicInfo 缓存键测试

    [Fact]
    public void BasicInfo_ShouldGenerateCorrectKey()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.BasicInfo(123);

        // Assert
        key.Key.Should().Be("ExamCacheOptions_BasicInfo_123");
    }

    [Fact]
    public void BasicInfo_ShouldHaveCorrectOptions()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.BasicInfo(123);

        // Assert
        key.Options.Should().NotBeNull();
        key.Options.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(30));
        key.Options.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(15));
        key.Options.Level.Should().Be(CacheLevel.Both);
    }

    [Fact]
    public void BasicInfo_ShouldHaveCorrectTags()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.BasicInfo(123);

        // Assert
        key.Tags.Should().NotBeNull();
        key.Tags.Should().HaveCount(1);
        key.Tags.Should().Contain("exam:123");
    }

    [Fact]
    public void BasicInfo_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var key1 = new ExamCacheOptions.BasicInfo(123);
        var key2 = new ExamCacheOptions.BasicInfo(123);

        // Assert
        key1.Should().Be(key2);
        (key1 == key2).Should().BeTrue();
    }

    [Fact]
    public void BasicInfo_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var key1 = new ExamCacheOptions.BasicInfo(123);
        var key2 = new ExamCacheOptions.BasicInfo(456);

        // Assert
        key1.Should().NotBe(key2);
        (key1 != key2).Should().BeTrue();
    }

    #endregion

    #region Questions 缓存键测试

    [Fact]
    public void Questions_ShouldGenerateCorrectKey()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.Questions(456);

        // Assert
        key.Key.Should().Be("ExamCacheOptions_Questions_456");
    }

    [Fact]
    public void Questions_ShouldHaveCorrectOptions()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.Questions(456);

        // Assert
        key.Options.Should().NotBeNull();
        key.Options.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(30));
        key.Options.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(15));
        key.Options.Level.Should().Be(CacheLevel.Both);
    }

    [Fact]
    public void Questions_ShouldHaveCorrectTags()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.Questions(456);

        // Assert
        key.Tags.Should().NotBeNull();
        key.Tags.Should().HaveCount(2);
        key.Tags.Should().Contain("exam:456");
        key.Tags.Should().Contain("questions");
    }

    #endregion

    #region UserRecord 缓存键测试

    [Fact]
    public void UserRecord_ShouldGenerateCorrectKey()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.UserRecord(789, 111);

        // Assert
        key.Key.Should().Be("ExamCacheOptions_UserRecord_789_111");
    }

    [Fact]
    public void UserRecord_ShouldHaveCorrectOptions()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.UserRecord(789, 111);

        // Assert
        key.Options.Should().NotBeNull();
        key.Options.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(10));
        key.Options.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(5));
        key.Options.Level.Should().Be(CacheLevel.Both);
    }

    [Fact]
    public void UserRecord_ShouldHaveCorrectTags()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.UserRecord(789, 111);

        // Assert
        key.Tags.Should().NotBeNull();
        key.Tags.Should().HaveCount(2);
        key.Tags.Should().Contain("exam:789");
        key.Tags.Should().Contain("user:111");
    }

    [Fact]
    public void UserRecord_WithSameParameters_ShouldBeEqual()
    {
        // Arrange
        var key1 = new ExamCacheOptions.UserRecord(789, 111);
        var key2 = new ExamCacheOptions.UserRecord(789, 111);

        // Assert
        key1.Should().Be(key2);
    }

    [Fact]
    public void UserRecord_WithDifferentParameters_ShouldNotBeEqual()
    {
        // Arrange
        var key1 = new ExamCacheOptions.UserRecord(789, 111);
        var key2 = new ExamCacheOptions.UserRecord(789, 222);

        // Assert
        key1.Should().NotBe(key2);
    }

    #endregion

    #region UserAnswers 缓存键测试

    [Fact]
    public void UserAnswers_ShouldGenerateCorrectKey()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.UserAnswers(999, 222);

        // Assert
        key.Key.Should().Be("ExamCacheOptions_UserAnswers_999_222");
    }

    [Fact]
    public void UserAnswers_ShouldHaveCorrectOptions()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.UserAnswers(999, 222);

        // Assert
        key.Options.Should().NotBeNull();
        key.Options.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(1));
        key.Options.SlidingExpiration.Should().Be(TimeSpan.FromSeconds(30));
        key.Options.Level.Should().Be(CacheLevel.Both);
    }

    [Fact]
    public void UserAnswers_ShouldHaveCorrectTags()
    {
        // Arrange & Act
        var key = new ExamCacheOptions.UserAnswers(999, 222);

        // Assert
        key.Tags.Should().NotBeNull();
        key.Tags.Should().HaveCount(2);
        key.Tags.Should().Contain("record:999");
        key.Tags.Should().Contain("user:222");
    }

    #endregion

    #region 键的唯一性测试

    [Fact]
    public void DifferentCacheKeyTypes_ShouldGenerateDifferentKeys()
    {
        // Arrange
        var basicInfoKey = new ExamCacheOptions.BasicInfo(123);
        var questionsKey = new ExamCacheOptions.Questions(123);

        // Assert
        basicInfoKey.Key.Should().NotBe(questionsKey.Key);
        basicInfoKey.Key.Should().Be("ExamCacheOptions_BasicInfo_123");
        questionsKey.Key.Should().Be("ExamCacheOptions_Questions_123");
    }

    [Fact]
    public void SameKeyType_WithDifferentIds_ShouldGenerateDifferentKeys()
    {
        // Arrange
        var key1 = new ExamCacheOptions.BasicInfo(123);
        var key2 = new ExamCacheOptions.BasicInfo(456);

        // Assert
        key1.Key.Should().NotBe(key2.Key);
    }

    #endregion
}

