# 题目类型特定字段实现说明

## 概述

本文档说明了问卷系统中各题型特定字段的后端实现逻辑。现在后端已经能够正确处理各种题型的创建和更新操作，包括题型特定的配置字段。

## 实现的功能

### 1. 题型特定字段处理

各题型的特定字段会被自动转换为JSON格式存储在Question实体的`Settings`字段中：

#### 评分题 (Rating)
- `RatingMin`: 评分最小值 (1-10)
- `RatingMax`: 评分最大值 (1-10)  
- `RatingStep`: 评分步长 (0.1-1)

#### 数字题 (Number)
- `NumberMin`: 数字最小值
- `NumberMax`: 数字最大值
- `NumberStep`: 数字步长

#### 文本题 (Text/Textarea)
- `TextMinLength`: 文本最小长度
- `TextMaxLength`: 文本最大长度
- `TextInputMode`: 输入模式 (text/email/tel/url/password)

#### 日期题 (Date/DateTime)
- `DateFormat`: 日期格式 (YYYY-MM-DD/YYYY/MM/DD等)
- `TimeFormat`: 时间格式 (HH:mm/HH:mm:ss等，仅DateTime和Time题型)

#### 时间题 (Time)
- `TimeFormat`: 时间格式

#### 矩阵题 (Matrix)
- `MatrixRows`: 行选项列表
- `MatrixColumns`: 列选项列表

### 2. 验证逻辑

#### 通用验证
- 选择题（单选、多选、排序）必须至少有2个选项
- 选项文本不能为空且不能重复
- 矩阵题必须至少有2个行选项和2个列选项

#### 题型特定验证
- **评分题**: 最小值必须小于最大值，范围1-10，步长0-1
- **数字题**: 最小值必须小于最大值，步长必须大于0
- **文本题**: 最小长度必须小于最大长度，最小长度不能小于0
- **矩阵题**: 行选项和列选项都不能为空

### 3. 选项处理

#### 普通选项
- 单选题、多选题、排序题的选项正常保存到QuestionOption表
- 支持选项的文本、值、排序和"其他"标记

#### 矩阵题选项
- 矩阵题的行选项作为普通选项保存
- 列选项信息保存在Settings JSON中

### 4. 更新逻辑

更新题目时会：
1. 删除所有现有选项
2. 重新创建新的选项
3. 更新题型特定设置
4. 保持数据一致性

## 使用示例

### 创建评分题

```json
{
  "surveyId": 1,
  "title": "服务满意度评分",
  "description": "请对我们的服务进行评分",
  "type": 5,
  "isRequired": true,
  "ratingMin": 1,
  "ratingMax": 5,
  "ratingStep": 1
}
```

### 创建矩阵题

```json
{
  "surveyId": 1,
  "title": "产品功能评价",
  "description": "请评价各项功能",
  "type": 10,
  "isRequired": true,
  "matrixRows": ["功能A", "功能B", "功能C"],
  "matrixColumns": ["非常满意", "满意", "一般", "不满意"],
  "options": [
    {"text": "功能A", "orderIndex": 0},
    {"text": "功能B", "orderIndex": 1},
    {"text": "功能C", "orderIndex": 2}
  ]
}
```

### 创建数字题

```json
{
  "surveyId": 1,
  "title": "年龄",
  "description": "请输入您的年龄",
  "type": 4,
  "isRequired": true,
  "numberMin": 18,
  "numberMax": 100,
  "numberStep": 1
}
```

## 数据库存储

题型特定字段以JSON格式存储在Question表的Settings字段中：

```json
{
  "ratingMin": 1,
  "ratingMax": 5,
  "ratingStep": 1
}
```

## 技术实现

### 核心方法

1. **BuildQuestionSettings()**: 将DTO中的题型特定字段转换为JSON
2. **ValidateQuestionTypeSpecificFields()**: 验证题型特定字段
3. **SaveQuestionOptionsAsync()**: 保存题目选项
4. **UpdateQuestionOptionsAsync()**: 更新题目选项

### AutoMapper配置

映射配置中忽略了题型特定字段，这些字段在服务层单独处理：

```csharp
.ForSourceMember(src => src.RatingMin, opt => opt.DoNotValidate())
.ForSourceMember(src => src.RatingMax, opt => opt.DoNotValidate())
// ... 其他题型特定字段
```

## 注意事项

1. 题型特定字段只在对应的题型中生效
2. Settings字段使用Newtonsoft.Json进行序列化
3. 选项更新采用先删除后创建的策略，确保数据一致性
4. 验证逻辑在创建和更新时都会执行

## 扩展性

如需添加新的题型或字段：

1. 在DTO中添加新字段并配置UI特性
2. 在`BuildQuestionSettings()`方法中添加处理逻辑
3. 在相应的验证方法中添加验证规则
4. 更新AutoMapper配置忽略新字段

这样的设计确保了系统的可扩展性和维护性。
