# 考试预生成方案测试要点

## 目录

- [1. 测试总览](#1-测试总览)
- [2. 单元测试](#2-单元测试)
- [3. 集成测试](#3-集成测试)
- [4. 功能测试](#4-功能测试)
- [5. 性能测试](#5-性能测试)
- [6. 边界条件测试](#6-边界条件测试)
- [7. 异常场景测试](#7-异常场景测试)
- [8. 数据一致性测试](#8-数据一致性测试)
- [9. 缓存测试](#9-缓存测试)
- [10. 定时任务测试](#10-定时任务测试)
- [11. 监控指标验证](#11-监控指标验证)
- [12. 测试环境准备](#12-测试环境准备)
- [13. 测试执行清单](#13-测试执行清单)

---

## 1. 测试总览

### 1.1 测试目标

验证考试预生成方案的以下核心能力：

- ✅ **功能完整性**：所有功能按预期工作
- ✅ **性能指标**：达到或超过预期性能目标
- ✅ **数据一致性**：数据在各种场景下保持一致
- ✅ **容错能力**：异常场景下系统正常降级
- ✅ **扩展性**：支持大规模和高并发场景

### 1.2 测试范围

| 测试类型 | 覆盖范围 | 优先级 |
|---------|---------|-------|
| 单元测试 | 核心服务方法 | P0 |
| 集成测试 | 服务间协作 | P0 |
| 功能测试 | 端到端流程 | P0 |
| 性能测试 | 响应时间、吞吐量 | P0 |
| 边界条件测试 | 极端场景 | P1 |
| 异常场景测试 | 容错能力 | P1 |
| 数据一致性测试 | 并发场景下数据正确性 | P0 |
| 缓存测试 | 缓存命中率、过期策略 | P0 |

### 1.3 测试环境

- **开发环境**：本地开发测试
- **测试环境**：完整功能测试、集成测试
- **压测环境**：性能测试、压力测试
- **生产环境**：灰度验证、监控验证

---

## 2. 单元测试

### 2.1 ExamRecordPreGenerationService 测试

#### 2.1.1 PreGenerateExamRecordsAsync 方法

**测试用例 UT-PRE-001：正常预生成单个考试**

```csharp
[Fact]
public async Task PreGenerateExamRecordsAsync_ValidExam_ShouldGenerateRecords()
{
    // Arrange
    var examId = 123L;
    var studentIds = new List<long> { 1, 2, 3 };
    
    // Act
    await _service.PreGenerateExamRecordsAsync(examId);
    
    // Assert
    // 1. 验证数据库中创建了3条记录
    var records = await _dbContext.ExamRecords
        .Where(r => r.ExamSettingId == examId && r.Status == ExamRecordStatus.NotStarted)
        .ToListAsync();
    Assert.Equal(3, records.Count);
    
    // 2. 验证每条记录的属性
    foreach (var record in records)
    {
        Assert.Equal(1, record.AttemptNumber);
        Assert.Equal(ExamRecordStatus.NotStarted, record.Status);
        Assert.Null(record.StartTime);
        Assert.Null(record.EndTime);
    }
    
    // 3. 验证缓存中写入了记录ID
    foreach (var studentId in studentIds)
    {
        var cacheKey = _service.GetPreGeneratedRecordCacheKey(examId, studentId, 1);
        var cachedRecordId = await _cache.GetAsync<long?>(cacheKey);
        Assert.NotNull(cachedRecordId);
    }
}
```

**测试用例 UT-PRE-002：考试已预生成，应跳过**

```csharp
[Fact]
public async Task PreGenerateExamRecordsAsync_AlreadyPreGenerated_ShouldSkip()
{
    // Arrange
    var examId = 123L;
    await _service.PreGenerateExamRecordsAsync(examId); // 第一次预生成
    
    // Act
    var result = await _service.PreGenerateExamRecordsAsync(examId); // 第二次预生成
    
    // Assert
    Assert.True(result.Skipped);
    Assert.Equal("已预生成，跳过", result.Message);
}
```

**测试用例 UT-PRE-003：分批预生成**

```csharp
[Fact]
public async Task PreGenerateBatchAsync_LargeStudentList_ShouldProcessInBatches()
{
    // Arrange
    var examId = 123L;
    var studentIds = Enumerable.Range(1, 150).Select(i => (long)i).ToList(); // 150名学生
    
    // Act
    await _service.PreGenerateBatchAsync(examId, studentIds, 1);
    
    // Assert
    // 1. 验证数据库中创建了150条记录
    var count = await _dbContext.ExamRecords
        .CountAsync(r => r.ExamSettingId == examId);
    Assert.Equal(150, count);
    
    // 2. 验证批次处理日志（应该有3批：50+50+50）
    _logger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("完成")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.Exactly(3)); // 3批
}
```

#### 2.1.2 GetPreGeneratedRecordCacheKey 方法

**测试用例 UT-KEY-001：缓存键格式正确**

```csharp
[Fact]
public void GetPreGeneratedRecordCacheKey_ValidInput_ShouldReturnCorrectFormat()
{
    // Arrange
    var examId = 123L;
    var studentId = 456L;
    var attemptNumber = 1;
    
    // Act
    var cacheKey = _service.GetPreGeneratedRecordCacheKey(examId, studentId, attemptNumber);
    
    // Assert
    Assert.Equal("exam:pregenerated:123:456:1", cacheKey);
}
```

### 2.2 ExamRecordService 测试

#### 2.2.1 CreateExamRecordAsync 方法

**测试用例 UT-CREATE-001：命中预生成记录，快速启动**

```csharp
[Fact]
public async Task CreateExamRecordAsync_PreGeneratedRecordExists_ShouldActivateQuickly()
{
    // Arrange
    var examId = 123L;
    var studentId = 456L;
    await _preGenService.PreGenerateExamRecordsAsync(examId);
    var startTime = DateTime.UtcNow;
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    var record = await _service.CreateExamRecordAsync(examId, studentId);
    stopwatch.Stop();
    
    // Assert
    // 1. 验证状态更新为InProgress
    Assert.Equal(ExamRecordStatus.InProgress, record.Status);
    Assert.NotNull(record.StartTime);
    Assert.True(record.StartTime >= startTime);
    
    // 2. 验证耗时在10-50ms范围内
    Assert.True(stopwatch.ElapsedMilliseconds < 50, 
        $"Expected < 50ms, actual: {stopwatch.ElapsedMilliseconds}ms");
    
    // 3. 验证缓存已清除
    var cacheKey = _preGenService.GetPreGeneratedRecordCacheKey(examId, studentId, 1);
    var cachedRecordId = await _cache.GetAsync<long?>(cacheKey);
    Assert.Null(cachedRecordId);
}
```

**测试用例 UT-CREATE-002：未命中预生成记录，动态创建**

```csharp
[Fact]
public async Task CreateExamRecordAsync_NoPreGeneratedRecord_ShouldCreateDynamically()
{
    // Arrange
    var examId = 123L;
    var studentId = 999L; // 新增学生，未预生成
    
    // Act
    var record = await _service.CreateExamRecordAsync(examId, studentId);
    
    // Assert
    // 1. 验证记录创建成功
    Assert.NotNull(record);
    Assert.Equal(ExamRecordStatus.InProgress, record.Status);
    Assert.NotNull(record.StartTime);
    
    // 2. 验证答题记录已创建
    var answerRecords = await _dbContext.ExamAnswerRecords
        .Where(r => r.ExamRecordId == record.Id)
        .ToListAsync();
    Assert.NotEmpty(answerRecords);
}
```

### 2.3 任务处理器测试

#### 2.3.1 ExamRecordScheduledPreGenerationTaskHandler

**测试用例 UT-TASK-001：定时任务正常执行**

```csharp
[Fact]
public async Task Execute_HasPublishedExams_ShouldPreGenerateAll()
{
    // Arrange
    var handler = _serviceProvider.GetRequiredService<ExamRecordScheduledPreGenerationTaskHandler>();
    var context = new ScheduledTaskContext
    {
        TaskId = "test-task",
        Parameters = null
    };
    
    // 创建3个已发布且尚未开始的考试
    await CreatePublishedExamsAsync(3);
    
    // Act
    var result = await handler.ExecuteAsync(context);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Contains("成功: 3", result.Message);
}
```

**测试用例 UT-TASK-002：跳过已预生成的考试**

```csharp
[Fact]
public async Task Execute_ExamAlreadyPreGenerated_ShouldSkip()
{
    // Arrange
    var examId = 123L;
    await _preGenService.PreGenerateExamRecordsAsync(examId);
    
    var handler = _serviceProvider.GetRequiredService<ExamRecordScheduledPreGenerationTaskHandler>();
    var context = new ScheduledTaskContext { TaskId = "test-task" };
    
    // Act
    var result = await handler.ExecuteAsync(context);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Contains("跳过: 1", result.Message);
}
```

#### 2.3.2 ExamRecordCleanupTaskHandler

**测试用例 UT-CLEANUP-001：清理未使用的记录**

```csharp
[Fact]
public async Task Execute_HasUnusedRecords_ShouldCleanup()
{
    // Arrange
    await CreateExpiredNotStartedRecordsAsync(10);
    
    var handler = _serviceProvider.GetRequiredService<ExamRecordCleanupTaskHandler>();
    var context = new ScheduledTaskContext
    {
        TaskId = "cleanup-task",
        Parameters = "{\"cleanupDays\": 7}"
    };
    
    // Act
    var result = await handler.ExecuteAsync(context);
    
    // Assert
    Assert.True(result.IsSuccess);
    
    // 验证记录已删除
    var count = await _dbContext.ExamRecords
        .CountAsync(r => r.Status == ExamRecordStatus.NotStarted);
    Assert.Equal(0, count);
}
```

---

## 3. 集成测试

### 3.1 完整预生成流程测试

**测试用例 IT-FLOW-001：发布考试 → 预生成 → 开始考试**

```
测试步骤：
1. 管理员发布考试（包含50名学生）
2. 等待定时任务执行或手动触发预生成
3. 验证数据库中创建了50条NotStarted记录
4. 验证缓存中写入了50个记录ID
5. 学生1开始考试，验证命中预生成记录
6. 验证学生1的记录状态更新为InProgress
7. 验证缓存中该学生的记录ID已清除

预期结果：
- 预生成成功率 100%
- 开始考试耗时 < 50ms
- 数据库和缓存状态一致
```

**测试用例 IT-FLOW-002：发布考试 → 预生成 → 新增学生 → 开始考试**

```
测试步骤：
1. 管理员发布考试（包含50名学生）
2. 执行预生成任务
3. 管理员新增10名学生到分组
4. 新增学生1开始考试
5. 验证执行动态创建逻辑

预期结果：
- 原50名学生命中预生成记录
- 新增10名学生使用动态创建
- 所有学生正常进入考试
```

### 3.2 多考试并发预生成测试

**测试用例 IT-CONCURRENT-001：同时预生成多个考试**

```
测试步骤：
1. 创建10个已发布的考试（每个100名学生）
2. 手动触发定时预生成任务
3. 监控预生成进度和日志
4. 验证所有考试预生成成功

预期结果：
- 10个考试全部预生成成功
- 总计创建1000条记录
- 任务执行时间 < 10分钟
- 无数据库死锁或超时
```

### 3.3 缓存与数据库一致性测试

**测试用例 IT-CONSISTENCY-001：缓存失效后重新查询**

```
测试步骤：
1. 预生成考试记录
2. 手动清除缓存
3. 学生开始考试
4. 验证从数据库查询到预生成记录

预期结果：
- 缓存失效后仍能正确查询数据库
- 自动重建缓存
- 学生正常进入考试
```

---

## 4. 功能测试

### 4.1 预生成功能测试

**测试用例 FT-PRE-001：预生成记录属性验证**

| 测试项 | 验证点 | 预期结果 |
|-------|-------|---------|
| 考试记录状态 | Status字段 | NotStarted |
| 尝试次数 | AttemptNumber字段 | 1 |
| 开始时间 | StartTime字段 | NULL |
| 结束时间 | EndTime字段 | NULL |
| 提交时间 | SubmittedAt字段 | NULL |
| 学生ID | StudentId字段 | 正确的学生ID |
| 考试ID | ExamSettingId字段 | 正确的考试ID |

**测试用例 FT-PRE-002：答题记录预生成验证**

```
测试步骤：
1. 创建包含10道题的考试
2. 执行预生成
3. 查询数据库中的答题记录

验证点：
- 每个学生创建10条答题记录
- 每条记录关联正确的题目ID
- 题目顺序正确（按DisplayOrder排序）
- 答案内容为空
- 得分为0
```

### 4.2 智能检测功能测试

**测试用例 FT-DETECT-001：缓存命中检测**

```
测试步骤：
1. 预生成考试记录
2. 学生开始考试
3. 检查日志输出

预期日志：
✅ 命中预生成记录，快速启动：考试ID=123, 学生ID=456, 记录ID=789
```

**测试用例 FT-DETECT-002：缓存未命中检测**

```
测试步骤：
1. 不执行预生成
2. 学生开始考试
3. 检查日志输出

预期日志：
⚠️ 未命中预生成记录，执行动态创建：考试ID=123, 学生ID=999
```

### 4.3 清理功能测试

**测试用例 FT-CLEANUP-001：清理已结束考试的未使用记录**

```
测试步骤：
1. 创建已结束的考试（EndTime = 昨天）
2. 预生成100条记录（Status = NotStarted）
3. 其中20名学生开始了考试（Status = InProgress）
4. 执行清理任务

预期结果：
- 删除80条NotStarted记录
- 保留20条InProgress记录
- 缓存同步清理
```

**测试用例 FT-CLEANUP-002：保留未结束考试的预生成记录**

```
测试步骤：
1. 创建未开始的考试（StartTime = 明天）
2. 预生成100条记录
3. 执行清理任务

预期结果：
- 不删除任何记录
- 缓存保持不变
```

---

## 5. 性能测试

### 5.1 响应时间测试

**测试用例 PT-TIME-001：开始考试响应时间（命中预生成）**

```
测试配置：
- 并发用户数：100
- 重复次数：每个用户5次
- 总请求数：500

性能指标：
- P50 响应时间 < 20ms
- P95 响应时间 < 50ms
- P99 响应时间 < 100ms
- 成功率 > 99.9%
```

**测试用例 PT-TIME-002：开始考试响应时间（未命中预生成）**

```
测试配置：
- 并发用户数：50（新增学生）
- 重复次数：每个用户2次
- 总请求数：100

性能指标：
- P50 响应时间 < 300ms
- P95 响应时间 < 500ms
- P99 响应时间 < 800ms
- 成功率 > 99%
```

### 5.2 吞吐量测试

**测试用例 PT-THROUGHPUT-001：预生成吞吐量**

```
测试配置：
- 考试规模：1000名学生
- 批次大小：50
- 批次延迟：200ms

性能指标：
- 预生成完成时间 < 5分钟
- CPU使用率 < 80%
- 数据库连接数 < 50
- 无超时或失败
```

**测试用例 PT-THROUGHPUT-002：开考峰值吞吐量**

```
测试配置：
- 同时开考：1000名学生
- 预生成比例：100%
- 压测时长：1分钟

性能指标：
- QPS > 200
- 响应时间 P95 < 100ms
- 成功率 > 99.5%
- 数据库CPU < 70%
```

### 5.3 对比测试

**测试用例 PT-COMPARE-001：预生成前后性能对比**

| 指标 | 预生成前 | 预生成后 | 提升 |
|-----|---------|---------|------|
| 开始考试P50响应时间 | 250ms | 15ms | 94% |
| 开始考试P95响应时间 | 450ms | 40ms | 91% |
| 最大并发支持 | 500人 | 1000人 | 100% |
| 数据库写入QPS峰值 | 200/s | 20/s | 90% |
| 缓存命中率 | - | 90%+ | - |

---

## 6. 边界条件测试

### 6.1 极端规模测试

**测试用例 BC-SCALE-001：超大规模考试（10000名学生）**

```
测试步骤：
1. 创建包含10000名学生的考试
2. 执行预生成任务
3. 监控执行时间和系统资源

预期结果：
- 预生成完成时间 < 1小时
- 批次处理正常（200批 × 50人/批）
- CPU使用率 < 90%
- 内存增长 < 2GB
- 无数据库死锁
```

**测试用例 BC-SCALE-002：多题目考试（500道题）**

```
测试步骤：
1. 创建包含500道题的考试
2. 预生成100名学生的记录
3. 验证答题记录创建完整

预期结果：
- 每个学生创建500条答题记录
- 总计50000条答题记录
- 预生成成功率 100%
```

### 6.2 时间边界测试

**测试用例 BC-TIME-001：开考前5分钟预生成**

```
测试步骤：
1. 创建考试（StartTime = 当前时间 + 10分钟）
2. 预生成1000名学生
3. 监控是否在开考前5分钟停止

预期结果：
- 前5分钟正常预生成
- 后5分钟停止预生成
- 日志记录停止原因和已处理数量
```

**测试用例 BC-TIME-002：考试已开始**

```
测试步骤：
1. 创建考试（StartTime = 当前时间 - 1小时，正在进行中）
2. 执行定时预生成任务

预期结果：
- 跳过该考试
- 日志记录跳过原因
```

**测试用例 BC-TIME-003：考试已结束**

```
测试步骤：
1. 创建考试（EndTime = 昨天）
2. 执行定时预生成任务

预期结果：
- 跳过该考试
- 日志记录跳过原因
```

### 6.3 缓存过期边界测试

**测试用例 BC-CACHE-001：缓存过期时间计算**

```
测试场景：
- 场景1：考试结束时间 = 7天后
  预期：缓存过期时间 = 7天 + 1小时

- 场景2：考试结束时间 = 1小时后
  预期：缓存过期时间 = 1小时 + 1小时

- 场景3：考试已结束
  预期：缓存过期时间 = 默认7天

- 场景4：考试结束时间异常（NULL或过去时间）
  预期：缓存过期时间 = 默认7天
```

---

## 7. 异常场景测试

### 7.1 数据库异常测试

**测试用例 EX-DB-001：数据库连接失败**

```
测试步骤：
1. 模拟数据库连接断开
2. 执行预生成任务

预期结果：
- 任务执行失败
- 记录详细错误日志
- 不影响其他服务
- 下次任务重试时恢复
```

**测试用例 EX-DB-002：事务超时**

```
测试步骤：
1. 设置极短的事务超时时间（1秒）
2. 预生成大批量数据（1000名学生）

预期结果：
- 单批次失败，记录错误
- 下一批次继续执行
- 部分学生预生成成功
- 失败学生开始考试时动态创建
```

**测试用例 EX-DB-003：唯一约束冲突**

```
测试步骤：
1. 预生成考试记录
2. 再次执行预生成（模拟重复执行）

预期结果：
- 检测到已预生成，跳过
- 不产生重复记录
- 日志记录跳过原因
```

### 7.2 缓存异常测试

**测试用例 EX-CACHE-001：缓存服务不可用**

```
测试步骤：
1. 停止Redis服务
2. 预生成考试记录
3. 学生开始考试

预期结果：
- 预生成任务记录警告但继续执行
- 开始考试时从数据库查询预生成记录
- 自动降级为无缓存模式
- 性能略有下降但功能正常
```

**测试用例 EX-CACHE-002：缓存数据不一致**

```
测试步骤：
1. 预生成考试记录
2. 手动删除数据库中的记录（但缓存仍存在）
3. 学生开始考试

预期结果：
- 检测到记录不存在
- 自动降级为动态创建
- 清理脏缓存
- 记录警告日志
```

### 7.3 并发冲突测试

**测试用例 EX-CONCURRENT-001：同一学生并发开始考试**

```
测试步骤：
1. 预生成学生A的考试记录
2. 学生A同时从2个设备开始考试（并发请求）

预期结果：
- 使用分布式锁防止并发
- 只有一个请求成功激活预生成记录
- 另一个请求等待或失败
- 数据库中只有1条InProgress记录
```

**测试用例 EX-CONCURRENT-002：预生成与开始考试并发**

```
测试步骤：
1. 启动预生成任务（处理1000名学生）
2. 部分学生在预生成过程中开始考试

预期结果：
- 已预生成的学生快速启动
- 未预生成的学生动态创建
- 无数据冲突
- 两种流程互不影响
```

### 7.4 数据完整性异常测试

**测试用例 EX-DATA-001：考试题目不存在**

```
测试步骤：
1. 创建考试但关联的题目被删除
2. 执行预生成任务

预期结果：
- 预生成失败
- 记录详细错误日志
- 不创建不完整的记录
- 管理员收到告警通知
```

**测试用例 EX-DATA-002：学生分组为空**

```
测试步骤：
1. 创建考试但不分配学生分组
2. 执行预生成任务

预期结果：
- 跳过该考试
- 日志记录：无需预生成（无学生）
```

---

## 8. 数据一致性测试

### 8.1 状态一致性测试

**测试用例 DC-STATE-001：状态转换验证**

```
测试步骤：
1. 预生成记录（Status = NotStarted）
2. 学生开始考试（Status = InProgress）
3. 学生提交考试（Status = Submitted）
4. 批改完成（Status = Graded）

验证点：
- 状态转换按预期顺序进行
- 不能从NotStarted直接到Submitted
- 时间戳正确记录
- 状态历史可追溯
```

**测试用例 DC-STATE-002：并发状态更新**

```
测试步骤：
1. 预生成记录
2. 多个线程同时更新记录状态

预期结果：
- 使用乐观锁或分布式锁
- 只有一个更新成功
- 数据状态一致
- 记录版本号正确递增
```

### 8.2 题目顺序一致性测试

**测试用例 DC-ORDER-001：预生成与动态创建题目顺序一致**

```
测试步骤：
1. 创建包含随机打乱题目的考试
2. 学生A使用预生成记录开始考试
3. 学生B使用动态创建开始考试
4. 对比两个学生的题目顺序

预期结果：
- 两个学生的题目顺序一致
- 题目顺序符合考试设置（是否打乱）
- 答题记录中的DisplayOrder正确
```

### 8.3 多租户隔离测试

**测试用例 DC-TENANT-001：租户数据隔离**

```
测试步骤：
1. 租户A发布考试A，预生成记录
2. 租户B发布考试B，预生成记录
3. 租户A的学生开始考试

预期结果：
- 只能访问租户A的考试记录
- 不能访问租户B的数据
- 缓存键包含租户ID（如需要）
- 数据库查询自动应用租户过滤
```

---

## 9. 缓存测试

### 9.1 缓存命中率测试

**测试用例 CACHE-HIT-001：预生成学生缓存命中率**

```
测试配置：
- 预生成1000名学生
- 1000名学生全部开始考试

性能指标：
- 缓存命中率 > 95%
- 未命中原因分析（缓存过期、清除等）
```

**测试用例 CACHE-HIT-002：新增学生缓存未命中**

```
测试配置：
- 预生成1000名学生
- 新增100名学生（未预生成）
- 全部1100名学生开始考试

性能指标：
- 缓存命中率 ≈ 90.9% (1000/1100)
- 新增学生全部动态创建
```

### 9.2 缓存过期测试

**测试用例 CACHE-EXPIRE-001：考试结束后缓存自动过期**

```
测试步骤：
1. 创建考试（EndTime = 1小时后）
2. 预生成记录（缓存过期时间 = 1小时 + 1小时 = 2小时）
3. 等待2小时后查询缓存

预期结果：
- 缓存自动过期
- 不占用内存
- 查询返回NULL
```

**测试用例 CACHE-EXPIRE-002：手动清理缓存**

```
测试步骤：
1. 预生成记录
2. 执行清理任务
3. 查询缓存

预期结果：
- 清理任务同步清理缓存
- 缓存中的预生成记录ID被删除
```

### 9.3 缓存容量测试

**测试用例 CACHE-CAPACITY-001：大规模缓存写入**

```
测试配置：
- 预生成100个考试
- 每个考试1000名学生
- 总计100,000条缓存记录

性能指标：
- 缓存写入成功率 > 99%
- Redis内存占用 < 500MB
- 缓存查询响应时间 < 5ms
```

---

## 10. 定时任务测试

### 10.1 定时预生成任务测试

**测试用例 CRON-PRE-001：Cron表达式验证**

```
测试配置：
- Cron表达式：0 0 1 * * *（每天凌晨1点）

验证步骤：
1. 配置任务并启动调度器
2. 监控任务执行时间
3. 验证任务按计划执行

预期结果：
- 任务在每天凌晨1点准时执行
- 执行误差 < 10秒
- 执行日志完整
```

**测试用例 CRON-PRE-002：任务超时处理**

```
测试配置：
- 任务超时时间：30分钟
- 预生成10,000名学生（预计需要40分钟）

预期结果：
- 任务执行30分钟后超时
- 记录超时日志
- 已处理的批次成功保存
- 下次任务继续处理未完成部分
```

**测试用例 CRON-PRE-003：任务重复执行防护**

```
测试步骤：
1. 执行预生成任务（耗时较长）
2. 在任务执行期间，手动再次触发任务

预期结果：
- 使用分布式锁防止重复执行
- 第二次触发失败或等待
- 日志记录：任务已在执行中
```

### 10.2 定时清理任务测试

**测试用例 CRON-CLEANUP-001：清理任务执行验证**

```
测试配置：
- Cron表达式：0 0 2 * * *（每天凌晨2点）
- 清理阈值：7天前

验证步骤：
1. 创建已结束7天的考试，包含100条NotStarted记录
2. 等待清理任务执行
3. 查询数据库

预期结果：
- 100条NotStarted记录全部删除
- 缓存同步清理
- 执行日志完整
```

**测试用例 CRON-CLEANUP-002：保留最近考试**

```
测试步骤：
1. 创建昨天结束的考试，包含100条NotStarted记录
2. 执行清理任务（清理阈值7天）

预期结果：
- 不删除任何记录（未超过阈值）
- 日志记录：无需清理
```

### 10.3 任务执行顺序测试

**测试用例 CRON-ORDER-001：预生成与清理任务顺序**

```
测试配置：
- 预生成任务：凌晨1点
- 清理任务：凌晨2点

验证点：
- 预生成任务在清理任务之前执行
- 两个任务不冲突
- 资源使用合理分配
```

---

## 11. 监控指标验证

### 11.1 性能指标监控

**监控指标 M-PERF-001：开始考试响应时间分布**

```
监控维度：
- P50响应时间
- P95响应时间
- P99响应时间
- 最大响应时间

告警阈值：
- P50 > 50ms 警告
- P95 > 100ms 告警
- P99 > 200ms 严重告警
```

**监控指标 M-PERF-002：预生成任务执行时间**

```
监控维度：
- 每批次处理时间
- 总执行时间
- 超时次数

告警阈值：
- 单批次处理 > 10秒 警告
- 总执行时间 > 30分钟 告警
- 超时次数 > 0 严重告警
```

### 11.2 功能指标监控

**监控指标 M-FUNC-001：预生成成功率**

```
计算公式：
成功率 = 成功预生成学生数 / 总学生数 × 100%

告警阈值：
- 成功率 < 95% 警告
- 成功率 < 90% 告警
- 成功率 < 80% 严重告警
```

**监控指标 M-FUNC-002：缓存命中率**

```
计算公式：
命中率 = 命中预生成记录次数 / 开始考试总次数 × 100%

告警阈值：
- 命中率 < 90% 警告
- 命中率 < 80% 告警
- 命中率 < 70% 严重告警
```

**监控指标 M-FUNC-003：提前停止频率**

```
监控维度：
- 因开考时间接近而停止的次数
- 停止时已处理/未处理学生比例

告警阈值：
- 停止次数 > 5次/天 警告
- 未处理学生比例 > 20% 告警
```

### 11.3 资源指标监控

**监控指标 M-RESOURCE-001：数据库连接数**

```
监控维度：
- 预生成时数据库连接数
- 峰值连接数
- 连接超时次数

告警阈值：
- 连接数 > 80% 最大值 警告
- 超时次数 > 0 告警
```

**监控指标 M-RESOURCE-002：Redis内存占用**

```
监控维度：
- 预生成缓存占用内存
- 总内存使用率
- 缓存写入失败次数

告警阈值：
- 缓存内存 > 1GB 警告
- 写入失败次数 > 0 告警
```

---

## 12. 测试环境准备

### 12.1 数据准备

**基础数据清单**

```sql
-- 1. 创建租户
INSERT INTO Tenants (Id, Name, Code) VALUES (1, '测试租户', 'test-tenant');

-- 2. 创建学生账号（1000个）
-- 使用脚本批量生成

-- 3. 创建考试模板
INSERT INTO ExamSettings (Id, Title, StartTime, EndTime, Duration, Status)
VALUES (1, '性能测试考试', '2025-01-01 09:00:00', '2025-01-01 11:00:00', 120, 1);

-- 4. 创建题目（100道）
-- 使用脚本批量生成

-- 5. 创建学生分组
INSERT INTO ExamStudentGroup (ExamSettingId, GroupName)
VALUES (1, '测试分组');

-- 6. 分配学生到分组
-- 使用脚本批量插入
```

### 12.2 环境配置

**配置文件示例（appsettings.Test.json）**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CodeSpirit_Test;..."
  },
  "Redis": {
    "Configuration": "localhost:6379,defaultDatabase=1"
  },
  "ScheduledTasks": {
    "Tasks": [
      {
        "Id": "exam-record-scheduled-pregeneration",
        "Name": "考试记录定时预生成",
        "HandlerType": "CodeSpirit.ExamApi.Tasks.ExamRecordScheduledPreGenerationTaskHandler",
        "CronExpression": "0 0 1 * * *",
        "Enabled": true,
        "Timeout": "00:30:00"
      },
      {
        "Id": "exam-record-cleanup",
        "Name": "考试记录垃圾数据清理",
        "HandlerType": "CodeSpirit.ExamApi.Tasks.ExamRecordCleanupTaskHandler",
        "CronExpression": "0 0 2 * * *",
        "Parameters": "{\"cleanupDays\": 7}",
        "Enabled": true
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "CodeSpirit.ExamApi": "Debug"
    }
  }
}
```

### 12.3 测试工具

**推荐工具列表**

| 工具 | 用途 | 说明 |
|-----|------|------|
| xUnit | 单元测试框架 | 编写单元测试和集成测试 |
| Moq | Mock框架 | 模拟依赖服务 |
| Bogus | 数据生成 | 生成测试数据 |
| NBomber | 性能测试 | 压力测试和性能测试 |
| BenchmarkDotNet | 基准测试 | 微基准测试和性能对比 |
| Redis Commander | Redis管理 | 查看和管理Redis缓存 |
| SQL Server Profiler | 数据库监控 | 监控数据库查询和性能 |

---

## 13. 测试执行清单

### 13.1 测试前检查

- [ ] 测试环境已部署并正常运行
- [ ] 数据库已创建并初始化
- [ ] Redis服务已启动
- [ ] 测试数据已准备（1000+学生，10+考试）
- [ ] 定时任务配置已验证
- [ ] 日志级别设置为Debug
- [ ] 监控系统已就绪

### 13.2 单元测试执行

- [ ] ExamRecordPreGenerationService测试（15个用例）
- [ ] ExamRecordService测试（10个用例）
- [ ] 任务处理器测试（6个用例）
- [ ] 缓存服务测试（5个用例）
- [ ] 单元测试覆盖率 > 80%

### 13.3 集成测试执行

- [ ] 完整预生成流程测试（2个场景）
- [ ] 多考试并发预生成测试（1个场景）
- [ ] 缓存与数据库一致性测试（1个场景）

### 13.4 功能测试执行

- [ ] 预生成功能测试（2个用例）
- [ ] 智能检测功能测试（2个用例）
- [ ] 清理功能测试（2个用例）

### 13.5 性能测试执行

- [ ] 响应时间测试（2个场景）
- [ ] 吞吐量测试（2个场景）
- [ ] 对比测试（1个场景）

### 13.6 边界条件测试执行

- [ ] 极端规模测试（2个用例）
- [ ] 时间边界测试（3个用例）
- [ ] 缓存过期边界测试（1个用例）

### 13.7 异常场景测试执行

- [ ] 数据库异常测试（3个用例）
- [ ] 缓存异常测试（2个用例）
- [ ] 并发冲突测试（2个用例）
- [ ] 数据完整性异常测试（2个用例）

### 13.8 数据一致性测试执行

- [ ] 状态一致性测试（2个用例）
- [ ] 题目顺序一致性测试（1个用例）
- [ ] 多租户隔离测试（1个用例）

### 13.9 缓存测试执行

- [ ] 缓存命中率测试（2个用例）
- [ ] 缓存过期测试（2个用例）
- [ ] 缓存容量测试（1个用例）

### 13.10 定时任务测试执行

- [ ] 定时预生成任务测试（3个用例）
- [ ] 定时清理任务测试（2个用例）
- [ ] 任务执行顺序测试（1个用例）

### 13.11 监控指标验证

- [ ] 性能指标监控（2个指标）
- [ ] 功能指标监控（3个指标）
- [ ] 资源指标监控（2个指标）

### 13.12 测试报告

- [ ] 测试执行记录完整
- [ ] 所有P0用例通过
- [ ] 缺陷已记录并分类
- [ ] 性能指标达标
- [ ] 测试报告已输出

---

## 14. 测试验收标准

### 14.1 功能验收

- ✅ 所有P0功能测试用例通过率 100%
- ✅ 所有P1功能测试用例通过率 > 95%
- ✅ 预生成成功率 > 95%
- ✅ 缓存命中率 > 85%

### 14.2 性能验收

- ✅ 开始考试P50响应时间（命中预生成） < 50ms
- ✅ 开始考试P95响应时间（命中预生成） < 100ms
- ✅ 并发1000人开考，成功率 > 99%
- ✅ 数据库写入压力降低 > 90%

### 14.3 稳定性验收

- ✅ 连续运行7天无严重故障
- ✅ 异常场景自动降级，成功率 > 95%
- ✅ 定时任务准时执行，误差 < 1分钟
- ✅ 资源占用稳定，无内存泄漏

### 14.4 数据一致性验收

- ✅ 数据一致性测试全部通过
- ✅ 并发场景无数据冲突
- ✅ 多租户数据完全隔离
- ✅ 题目顺序100%正确

---

## 15. 附录

### 15.1 测试数据生成脚本

**生成1000名学生账号**

```csharp
public class TestDataGenerator
{
    public async Task GenerateStudentsAsync(int count)
    {
        var faker = new Faker<Student>()
            .RuleFor(s => s.Name, f => f.Person.FullName)
            .RuleFor(s => s.Email, f => f.Person.Email)
            .RuleFor(s => s.StudentNumber, f => f.Random.String2(10, "0123456789"))
            .RuleFor(s => s.TenantId, f => 1);

        var students = faker.Generate(count);
        await _dbContext.Students.AddRangeAsync(students);
        await _dbContext.SaveChangesAsync();
    }
}
```

**生成考试和题目**

```csharp
public async Task GenerateExamAsync(int questionCount)
{
    var exam = new ExamSetting
    {
        Title = "性能测试考试",
        StartTime = DateTime.UtcNow.AddDays(1),
        EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
        Duration = 120,
        Status = ExamSettingStatus.Published
    };
    
    await _dbContext.ExamSettings.AddAsync(exam);
    await _dbContext.SaveChangesAsync();
    
    var questions = new Faker<ExamQuestion>()
        .RuleFor(q => q.ExamSettingId, f => exam.Id)
        .RuleFor(q => q.Content, f => f.Lorem.Sentence())
        .RuleFor(q => q.Score, f => 10)
        .Generate(questionCount);
        
    await _dbContext.ExamQuestions.AddRangeAsync(questions);
    await _dbContext.SaveChangesAsync();
}
```

### 15.2 性能测试脚本

**NBomber压测脚本示例**

```csharp
public class ExamStartLoadTest
{
    public void Run()
    {
        var scenario = Scenario.Create("start_exam", async context =>
        {
            var examId = 123L;
            var studentId = Random.Shared.Next(1, 1001); // 1000名学生
            
            var response = await _httpClient.PostAsync(
                $"/api/exam-records/start",
                new StringContent(JsonSerializer.Serialize(new { examId, studentId }))
            );
            
            return response.IsSuccessStatusCode 
                ? Response.Ok() 
                : Response.Fail();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 200, during: TimeSpan.FromMinutes(1))
        );
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}
```

### 15.3 监控Dashboard配置

**Grafana Dashboard JSON示例**

```json
{
  "dashboard": {
    "title": "考试预生成监控",
    "panels": [
      {
        "title": "开始考试响应时间",
        "targets": [
          {
            "expr": "histogram_quantile(0.50, exam_start_duration_seconds)"
          },
          {
            "expr": "histogram_quantile(0.95, exam_start_duration_seconds)"
          }
        ]
      },
      {
        "title": "缓存命中率",
        "targets": [
          {
            "expr": "rate(exam_cache_hits_total[5m]) / rate(exam_cache_total[5m]) * 100"
          }
        ]
      },
      {
        "title": "预生成成功率",
        "targets": [
          {
            "expr": "rate(exam_pregeneration_success_total[5m]) / rate(exam_pregeneration_total[5m]) * 100"
          }
        ]
      }
    ]
  }
}
```

---

> 📝 **文档版本**：v1.0  
> 📅 **最后更新**：2025年12月  
> 👤 **维护团队**：CodeSpirit 开发团队  
> 🔗 **相关文档**：[考试预生成方案](./exam-record-pregeneration-zh-CN.md)

