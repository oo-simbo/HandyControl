# 修改与验证台账

## HC-M002：WPF 多语言收敛为简体中文和英文

- 基线：`f1b8be08221f20d0e0a0273cd70301e99dbe3589`。
- 删除库与 Demo 各 11 个其他语言 resx，移除对应项目资源项、Demo 语言按钮及 11 个旗帜图片。
- 保留中文默认资源 `Lang.resx` 和英文 `Lang.en.resx`。中文仍内置于主 DLL，英文输出为 `en/HandyControl.resources.dll`；不新增重复中文卫星包。
- Demo 启动时将已保存的其他语言配置回退为 `zh-cn`，英文区域归一为 `en`。
- Avalonia 无修改。Cake 现有语言枚举基于 resx 文件，因此现在只枚举英文，无须新增硬编码语言名单。
- 验证：clean 后 WPF Demo 及引用项目 Release 全量编译，0 警告、0 错误（11.43 秒）。库和 Demo 输出均仅存在英文卫星资源。
- 库中英文各 40 个资源键、Demo 中英文各 231 个资源键，英文键无缺失。
- 三种皮肤下验证 `zh-cn → en → en-US → zh-cn`，确认资源读取、动态绑定更新及英文卫星程序集加载；原有窗口、NumericUpDown、Growl 冒烟检查全部通过。
- Git diff --check 通过。未执行下游实际应用升级或完整 Demo 人工交互。
- 部署路径、显式复制配置和运行时切换方式见[中英文语言包与切换](build-and-use.md#中英文语言包与切换)。

## HC-M001：确立 .NET 10 WPF 维护基线

- 状态：代码、维护文档及自动冒烟验证已完成；按维护者要求将本批改动整理为一个提交，提交记录以 Git 历史为准。
- 需求：个人库用于吸收官方未合并 PR、修复未解决 Bug；下游直接使用编译后的 DLL。最低 .NET 10，先维护 WPF，Avalonia 后续处理。
- 个人基线：`c67b8eba8d697652236ebf2787be1f2c5b48c9e7`，`main`。
- 来源：个人维护政策与本地代码调查；不是某个官方 PR 的合并记录。

### 实际修改

1. WPF 库、Demo、DemoCode 统一使用 `Microsoft.NET.Sdk` 和单目标 `net10.0-windows`；删除旧 Framework 引用程序集包、旧 TFM 警告配置及 net4 专用构建动作。
2. 删除 .NET 4.0 专用项目/资源、旧 `Microsoft.Windows.Shell` 兼容实现、`Theme_40.txt`、未参与现代 Demo 编译的旧 AssemblyInfo，以及 Net40 Rider 启动配置。合计删除 171 个跟踪文件；大部分删除行来自旧 Shell 实现及旧主题副本。
3. 清理 solution 的 Net40 配置及旧 Shell 项目映射；保留 `Net_GE45` 路径以方便后续与上游定位。Rider 的 WPF Demo 启动目标由 net9 更新为 net10。
4. WPF 核心及 Demo 中删除旧版本条件分支，直接使用当前 .NET 10 生效的绑定、虚拟化、调用者属性名和窗口实现。Demo 版本显示改为 `RuntimeInformation.FrameworkDescription`。
5. `NumericUpDown.SelectionTextBrush` 原被 `NET48_OR_GREATER` 包围，该符号在 .NET 10 不生效。现在公开此依赖属性、CLR 属性及模板 TextBox 的绑定。这是本次明确的 API/行为增量，其余条件清理保持现代分支逻辑。
6. Cake 构建矩阵仅保留 `net10.0-windows`，NuGet 目标组和产物目录也使用该 Windows TFM。删除 Framework/Core 包文件分支，GitHub 资产路径改为按矩阵枚举，不再固定 net40。
7. 增加维护范围、架构导航、DLL 构建接入、PR/Bug 流程以及可重复执行的二进制冒烟程序。

### 保留的范围边界

- `src/Avalonia` 无修改，Avalonia 启动配置也未修改。
- 现有 Expression/Interactivity 共享代码仍参与 WPF 编译，保留。
- 保留必要的 Windows 版本、DPI、窗口行为判断，以及程序集名称、XMLNS、资源 URI、版本和签名密钥。
- 现有 Hexo 上游使用文档与 Visual Studio 历史模板不在本次运行库迁移范围。
- Squirrel 安装包布局 `lib/net45` 是旧工具的目录契约；不属于库目标框架兼容代码。Demo 中 Dotnet9 网站名称及链接也不是 TFM 配置。

### 验证环境与结果

环境：Windows x64，SDK `10.0.302`，MSBuild `18.6.11`，WindowsDesktop Runtime `10.0.10`。记录按本次维护会话整理；机器日志日期与会话日期不同，因此不把会话日期当作精确构建时间戳。

| 检查 | 结果 |
| --- | --- |
| 修改前 WPF 库 Release 基线 | 0 警告、0 错误 |
| 修改前 WPF Demo Release 基线 | 0 警告、0 错误 |
| 最终 WPF 库 Release 全量重编译 | 0 警告、0 错误；2.85 秒 |
| 最终 WPF Demo 及引用项目 Release 全量重编译 | 0 警告、0 错误；4.01 秒 |
| Git diff --check / 维护文档相对链接 | 通过 / 无失效链接 |
| 直接引用 Release DLL 的 STA 冒烟程序 | 成功，退出码 0 |
| SkinDefault / SkinDark / SkinViolet 资源加载 | 三种全部通过 |
| NumericUpDown 默认 / Extend / Plus 模板 | 每种皮肤均验证三个不同模板，PART_TextBox 存在 |
| SelectionTextBrush 初值与运行时更新 | 均传播到模板 TextBox |
| NumericUpDown 文本输入及上下界 | 输入 7 更新 Value；99 限制为 10，-99 限制为 -10 |
| WindowWin10 模板与 System.Windows.Shell.WindowChrome | 窗口创建、布局和关闭通过 |
| Growl 指定面板消息创建与清除 | 消息内容及子项数量断言通过 |
| 活动 WPF/build 源码旧框架扫描 | 无旧条件分支或被删项目引用；保留上述 Squirrel 与网站名称 |
| Avalonia 源码差异 | 无 |

验证命令和覆盖范围见[构建与二进制接入](build-and-use.md)。首次冒烟脚本误用 `typeof(HandyControl.Controls.Window)` 查找样式；读取现有实现确认实际键为 `WindowWin10` 后修正脚本，未因此修改生产窗口行为。

当前已验证库 DLL SHA256：

```text
52D2F290D9EA4966EB9890E110D24C2A5BA003D23309046B2F90E9838D1DBBEB
```

对应 `src/Net_GE45/HandyControl_Net_GE45/bin/Release/net10.0-windows/HandyControl.dll`，程序集版本仍为 `3.6.0.0`。该哈希标识本次产物，重新构建后需重新记录，不是未来版本承诺。

### 尚未验证 / 后续维护

- 下游实际项目没有提供，尚未替换其 DLL 或执行接入回归。
- 未进行完整 Demo 人工交互、多屏不同 DPI、窗口最大化/任务栏边界、输入法、语言切换及通知动画/计时回归。
- Cake/Squirrel 安装包、NuGet 和 GitHub 发布未执行；相关脚本仅完成本次范围内静态修改，仍需后续单独配置个人发布目标。
- 未合并具体官方 PR，也未发布安装包、NuGet 包或 GitHub Release。远程同步状态以 Git 为准。

### 回退

本批维护基线清理、控件行为修复及文档按维护者要求合并为一个提交。后续可按该提交回退，单项回退需核对代码和文档的依赖关系。下游分发前先归档旧 DLL 和语言资源，回退时整批恢复。不得通过覆盖整个工作区来回退后续无关修改。
