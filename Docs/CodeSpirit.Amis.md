# 智能界面生成引擎——功能说明

### 后台前端

- [x] 站点导航

  - [x] 动态生成
    - [x] 支持PageAttribute

    - [x] 支持配置文件配置（PagesConfiguration）

    - [x] 根据权限生成
  - [ ] 全局导航

- [x] 增删改查

  - [x] 基于QueryDto生成查询表单

    - [x] 字段类型
      - [x] bool——>switch
      - [x] enum——>select
      - [x] string——>text
      - [x] DateTime[]——>日期范围
      - [ ] Int[]——>整数范围
  - [x] 基于列表方法生成列表

    - [x] 分页
    - [x] 支持嵌套
    - [x] 列
      - [x] 列特性：ColumnAttribute
      - [x] 列类型
        - [x] enum——>mapping
        - [x] bool——>switch
        - [x] `DateTime`|`DateTimeOffset`|`DateColumnAttribute`——>date
        - [x]  `AvatarColumnAttribute`|包含Avatar列|——>avatar
        - [x] `[DataType(DataType.ImageUrl)]` | 包含Image列——>image
        - [x] `List<T> where T:calss`——>List 
        - [x] `TplColumnAttribute`——>tpl
        - [x] `LinkColumnAttribute`——>link
        - [ ] 状态
        - [x] `BadgeAttribute`（仅支持tpl、link、avatar）
        - [x] JSON
      - [x] 列排序
        - [x] 支持配置
      - [x] 列忽略：IgnoreColumnAttribute
      - [x] 默认隐藏
        - [x] 主键
        - [x] 密码
      - [x] 操作列
        - [x] 查看
          - [x] 自动添加分割线
          - [x] 头像、图片类型自动对齐
        - [x] 编辑
        - [x] 删除
        - [x] 自定义操作：OperationAttribute
          - [x] 请求成功跳转（参考模拟登录）
        - [ ] 根据权限控制操作按钮
      - [x] 快速编辑
        - [x] 只读列
      - [x] 背景色阶
      - [ ] 单元格样式
      - [ ] 默认是否显示
      - [ ] 固定列
    - [x] 头部操作
      - [x] 添加
      - [x] 批量导入
        - [x] Excel上传解析
        - [x] 导入预览
          - [x] 显示序号
        - [x] 导入数据修改
          - [x] 支持删除行
          - [x] 支持添加行
          - [x] 导入编辑控件支持
            - [x] 同表单控件转换
        - [x] 导入数据验证
          - [x] 必填验证
          - [x] 范围验证
          - [x] 正则验证
      - [x] 导出Excel
        - [x] 导出当前页
        - [x] 导出全部
        - [ ] 后端导出（Magicodes.IE)
      - [ ] 导出Csv
        - [ ] 导出当前页
        - [ ] 导出全部
        - [ ] 后端导出（Magicodes.IE)
      - [x] 批量操作
        - [x] 批量删除
        - [ ] 批量表单操作
    - [x] 底部信息及操作
      - [x] 切换分页数
      - [x] 记录数展示
    - [x] 操作表单（添加、编辑等）
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
        - [x] 密码字段：`DataTypeAttribute`|`DataType.Password`
        - [x] InputTree
        - [x] Excel上传及解析：`AmisInputExcelFieldAttribute` 
        - [ ] JSON
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
      - [x] 字段验证
        - [x] 必填
        - [x] 文本长度验证
        - [x] 数值范围验证
        - [ ] 日期范围验证
        - [x] 正则表达式
        - [x] 特殊类型验证（Email、Url等）
          - [x] DataType.EmailAddress
          - [x] DataType.Url
          - [x] DataType.PhoneNumber
          - [x] DataType.PostalCode
          - [x] DataType.ImageUrl
        - [x] 自定义错误消息
          - [x] `[RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线。")]`
      - [ ] 并发控制
      - [x] 支持字段描述：`DescriptionAttribute`

- [x] 图表

  - [x] 支持日期筛选

## H5