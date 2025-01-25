# CodeSpirit（码灵）低代码框架

CodeSpirit（码灵）是一款高效的后台低代码框架，旨在通过后端代码自动生成前端页面代码，显著减少页面开发与对接的工作量，从而大幅提升开发效率。接下来，CodeSpirit 将进一步支持后端代码自动生成，并结合 AI 技术，提供更多智能化的赋能，助力开发者更轻松地构建高质量应用。

## 优势

相比其他低代码框架或平台，CodeSpirit有如下优势：

1. 代码完全可控
2. 高性能（可以不断优化后端代码）
3. 代码可读性高，易于维护
4. 既支持低代码，亦支持高度自定义
5. 框架易于扩展

## 动态代码生成

### 后台前端

- [ ] 站点导航

- [ ] 增删改查

  - [ ] 基于QueryDto生成查询表单

    - [ ] 字段类型
      - [x] bool——>switch
      - [x] enum——>select
      - [x] string——>text
      - [x] DateTime[]——>日期范围
      - [ ] Int[]——>整数范围

  - [ ] 基于列表方法生成列表

    - [x] 分页
    - [ ] 支持嵌套
    - [ ] 列
      - [x] 列类型
        - [x] enum——>mapping
        - [x] bool——>switch
        - [x] DateTime|DateTimeOffset——>datetime
        - [x] [DataType(DataType.ImageUrl)] | 包含Avatar列——>avatar
        - [x] [DataType(DataType.ImageUrl)] | 包含Image列——>image
        - [ ] List<T> where T:calss——>List 
      - [ ] 列排序
      - [x] 默认隐藏
        - [x] 主键
        - [x] 密码
      - [ ] 操作列
        - [ ] 查看
        - [x] 编辑
        - [x] 删除
        - [x] 自定义操作：OperationAttribute
        - [ ] 根据权限控制操作按钮
      - [x] 快速编辑
        - [ ] 只读列
    - [ ] 头部操作
      - [x] 添加
      - [ ] 批量导入
      - [ ] 导出Excel
      - [ ] 导出Csv
      - [ ] 批量操作
        - [ ] 批量删除
    - [ ] 操作表单（添加、编辑等）
      - [ ] 权限控制
      - [x] 字段支持
        - [x] 图片字段：AmisInputImageFieldAttribute
        - [x] 下拉列表：AmisSelectFieldAttribute
        - [x] 自定义字段：AmisFieldAttribute
        - [x] enum——>select
        - [x] 文本字段
        - [x] int|long|float|double——>number
        - [x] bool——>switch
        - [x] DateTime|DateTimeOffset——>datetime
        - [x] 密码字段：DataTypeAttribute|DataType.Password
        - [ ] 数组
        - [ ] 文件
        - [ ] 城市选择器
        - [ ] 颜色选择器
        - [ ] 键值对
        - [ ] 对比编辑器
        - [ ] 地理位置选择
        - [ ] 评分
        - [ ] 标签
        - [ ] 签名
      - [ ] 字段验证
        - [x] 必填
        - [x] 文本长度验证
        - [x] 数值范围验证
        - [ ] 日期范围验证
        - [ ] 正则表达式

### 后台代码

- [ ] 生成方式
  - [ ] 基于模型生成
  - [ ] 函数配置
- [ ] 仓储
- [ ] 控制器
  - [ ] 查询
  - [ ] 单个查询
  - [ ] 创建
  - [ ] 编辑
  - [ ] 删除
  - [ ] 批量更新
  - [ ] 导出



## 后端框架

- [x] .NET 9
  - [x] .NET Aspire
  - [x] 前后端集成
- [ ] Dapr
- [ ] 容器支持
- [ ] 配置中心集成
- [ ] 日志服务
- [ ] 审计服务
- [ ] ORM封装
  - [ ] 实体基类
  - [ ] 事件触发器
  - [x] 数据筛选器
    - [x] 租户筛选器
    - [x] 软删筛选器
- [ ] 组件封装
  - [ ] API包装
  - [x] 全局异常处理
  - [ ] 审计
  - [ ] 雪花Id
  - [ ] 服务自动注册
    - [ ] IScopedDependency
    - [ ] ITransientDependency
    - [ ] ISingletonDependency
  - [ ] 导入导出
    - [ ] 集成Magicodes.IE

## 内置模块开发

### 用户中心

- [ ] 登录
- [ ] 用户管理
- [ ] 角色管理
- [ ] 权限管理
- [ ] 租户管理

## DevOps

## 数据洞察

## 未来

低代码+AI