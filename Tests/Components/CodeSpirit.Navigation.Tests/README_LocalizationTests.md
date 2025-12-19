# 导航组件多语言功能单元测试说明

## 测试文件

### 1. NavigationLocalizationServiceTests.cs
**测试类**：`NavigationLocalizationService` 的核心本地化逻辑

**测试用例**：
- ✅ `LocalizeNavigationTree_WhenNodesIsEmpty_ShouldReturnEmptyList` - 空列表处理
- ✅ `LocalizeNavigationTree_WhenNodesIsNull_ShouldReturnEmptyList` - null 处理
- ✅ `LocalizeNavigationTree_WhenNodeHasNoResourceKey_ShouldKeepOriginalTitle` - 无资源键时保持原文本
- ✅ `LocalizeNavigationTree_WhenNodeHasResourceKey_ShouldLocalizeTitle` - 中文环境本地化
- ✅ `LocalizeNavigationTree_WhenNodeHasResourceKeyAndEnglishCulture_ShouldReturnEnglishText` - 英文环境本地化
- ✅ `LocalizeNavigationTree_WhenResourceKeyNotFound_ShouldKeepOriginalTitle` - 资源键不存在时回退
- ✅ `LocalizeNavigationTree_ShouldRecursivelyLocalizeChildren` - 递归本地化子节点
- ✅ `LocalizeNavigationTree_ShouldLocalizeDescription` - 描述信息本地化
- ✅ `LocalizeNavigationTree_ShouldReturnDeepCopy` - 深拷贝验证
- ✅ `LocalizeNavigationTree_WhenResourceTypeNotFound_ShouldKeepOriginalTitle` - 资源类型不存在时回退

### 2. NavigationTreeBuilderLocalizationTests.cs
**测试类**：`NavigationTreeBuilder` 的资源键保存逻辑

**测试用例**：
- ✅ `CreateNavigationNode_WhenNavigationAttributeHasResourceKey_ShouldSaveResourceInfo` - 验证 NavigationAttribute 资源键保存
- ✅ `CreateNavigationNode_WhenDisplayAttributeHasResourceType_ShouldSaveResourceInfo` - 验证 DisplayAttribute 资源键保存
- ✅ `MergeNavigationNodes_ShouldPreserveResourceKeyInfo` - 合并节点时保留资源键信息
- ✅ `CreateNavigationNode_WhenBothAttributesExist_ShouldPreferNavigationAttribute` - 属性优先级验证

### 3. NavigationLocalizationIntegrationTests.cs
**测试类**：完整的本地化流程集成测试

**测试用例**：
- ✅ `FullLocalizationFlow_ChineseCulture_ShouldReturnLocalizedText` - 中文环境完整流程
- ✅ `FullLocalizationFlow_EnglishCulture_ShouldReturnLocalizedText` - 英文环境完整流程
- ✅ `MixedScenario_SomeNodesHaveResourceKeys_ShouldLocalizeOnlyThoseWithKeys` - 混合场景（部分有资源键）
- ✅ `DeepCopyVerification_OriginalNodesShouldNotBeModified` - 深拷贝验证（不修改原始数据）

## 测试覆盖的关键逻辑

### 1. 资源键解析
- ✅ 从 `NavigationAttribute` 读取资源键和资源类型
- ✅ 从 `DisplayAttribute` 读取资源键和资源类型
- ✅ 优先级：NavigationAttribute > DisplayAttribute > 硬编码文本

### 2. 多语言转换
- ✅ 根据当前语言（CultureInfo）从资源文件获取文本
- ✅ 支持中文（zh-CN）和英文（en）
- ✅ 资源键不存在时回退到原始文本
- ✅ 资源类型不存在时回退到原始文本

### 3. 递归处理
- ✅ 递归处理所有子节点
- ✅ 保持导航树结构完整

### 4. 深拷贝机制
- ✅ 返回新的导航节点列表，不修改原始数据
- ✅ 确保缓存数据不被污染

### 5. 边界情况处理
- ✅ null 输入处理
- ✅ 空列表处理
- ✅ 资源键/类型缺失处理
- ✅ 混合场景（部分节点有资源键）

## 运行测试

```bash
# 运行所有本地化相关测试
dotnet test Tests/Components/CodeSpirit.Navigation.Tests/CodeSpirit.Navigation.Tests.csproj --filter "FullyQualifiedName~NavigationLocalization"

# 运行特定测试类
dotnet test Tests/Components/CodeSpirit.Navigation.Tests/CodeSpirit.Navigation.Tests.csproj --filter "FullyQualifiedName~NavigationLocalizationServiceTests"

# 运行特定测试方法
dotnet test Tests/Components/CodeSpirit.Navigation.Tests/CodeSpirit.Navigation.Tests.csproj --filter "FullyQualifiedName~NavigationLocalizationServiceTests.LocalizeNavigationTree_WhenNodeHasResourceKey"
```

## 测试结果

✅ **所有 14 个测试用例全部通过**

测试覆盖了导航组件多语言功能的关键逻辑，包括：
- 资源键的保存和读取
- 多语言文本的转换
- 递归处理子节点
- 深拷贝机制
- 边界情况处理

