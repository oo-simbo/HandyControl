# 修改与验证台账

## HC-M005：发布 3.6.2.0 DLL 并部署到 JuLink.Ultron（2026-09-14）

- 版本：Version、FileVersion、AssemblyVersion 从 3.6.1.0 更新为 3.6.2.0，包含 HC-M004 的两个 PR 适配和 Demo。发布指本地 Release 二进制交付，未创建 NuGet/GitHub Release。
- 部署：同批 `HandyControl.dll`、`HandyControl.pdb`、`HandyControl.xml`、`en/HandyControl.resources.dll` 已复制到 `D:/Sourcecode/JuLink.Ultron/src/JuLink.Common.UI.Wpf/Dlls`；逐文件 SHA256 校验一致。保留现有主程序集 Reference 和 en 自动复制配置，无需修改 csproj。
- 部署补充：按用户要求，已移除 Dlls、业务 Debug/Release 和发布验证目录中的 `HandyControl.pdb`，逐路径确认不存在；后续不向下游分发 PDB。XML 保留为可选 IDE 文档，不是运行依赖。本次不改变 DLL 及其哈希。
- 回退备份：`D:/Develop/HandyControl/artifacts/deployment-backups/Ultron-before-3.6.2.0-20260914-134842`，保存替换前 Dlls 内容。回退时恢复旧主 DLL 和 en 包，移除本批新增的 PDB/XML 后重建下游，避免调试符号版本混用。
- 验证：WPF 库、Demo、DemoCode Release 全量构建 0 警告、0 错误；三皮肤控件冒烟及三个已编译 Demo 页面验证通过。JuLink.Common.UI.Wpf Release/Debug 全量构建分别为 510 警告、0 错误（nullable 等；未建立本轮替换前对照）；Release publish --no-build 成功。业务 Debug、Release、发布验证目录中的主 DLL 和 en 卫星版本均为 3.6.2.0，哈希与 Dlls 一致。
- 主 DLL SHA256：`DEC9D6699324A59B99DB40C01C7A68D681BCE75A2BE914D21AE8D89A64BAC19E`。
- 英文卫星 SHA256：`4D47126217F9F5F9B3BF35A967AC744C8A6EFE4F1C5286EFCD0B60A07EE184E3`。
- 日志及发布验证产物保留于本库 `artifacts/ultron-hc-3.6.2.0-*`；旧版本备份不是待提交源码。
- 提交整理：PropertyGrid 补丁提交 `4f68968`，ClockType 补丁提交 `7b878b5`，Ultron 二进制接入提交 `de02b3f`；版本和维护文档另行提交。原有主题、solution、作者元数据等修改保留在工作区，交付 DLL 来自此前验证的工作区构建，不声称仅检出上述提交即可得到相同哈希。生成 XML 第 707 行保留源注释的行尾空格以维持产物一致，其余暂存文件格式检查通过。远程结果统一记录在 JAX 总台账。
- 状态：已完成本地二进制交付和提交整理。未执行完整 JAX/Ultron 解决方案构建或业务宿主人工交互；既有 WPF 业务测试问题不因本次库升级而视为解决。下游契约更新在所属工程文档，交付状态同步到 JAX 总台账。

## 官方本地副本信息更正（2026-09-14）

- 经 Git 命令核查，`D:/Develop/HandyControl-master` 存在 `.git`，是官方仓库的本地 Git 副本；`origin` 为 `https://github.com/HandyOrg/HandyControl.git`。
- 分支 `master`；HEAD 与本地 `origin/master` 均为 `2c0875ebd67326e0c67282967e3e809c69282fee`；工作区干净。本次未 fetch，不据此声明与远端最新状态同步。
- 已更新维护范围和 PR 维护流程，纠正此前“无 Git 元数据”的判断；说明可使用本地历史追踪上游来源，且 cherry-pick 不以共同祖先为必要条件。此前两个 PR 的源码移植结果不受此信息更正影响。
- 本次仅更新文档，不修改控件、Demo 或官方副本。

## HC-M004：移植官方 PropertyGrid 排序与 Clock/ListClock 选择 PR

- 来源一：PR [#1794](https://github.com/HandyOrg/HandyControl/pull/1794)，作者 `katway`，head `1ea84e2b8234ffeaef951ab88384b12373b2ad11`；截至 2026-09-14 状态为 open、未合并。该 PR 增加 PropertyGrid 的 CategoryOrder/PropertyOrder，并让枚举编辑器优先显示 `DescriptionAttribute`。
- 来源二：PR [#1299](https://github.com/HandyOrg/HandyControl/pull/1299)，作者 `QyQj`，commit `cb3d219bc5329fcb8ef7153e540cc5178ba594d1`；截至 2026-09-14 状态为 closed、未合并；关联 Issue [#629](https://github.com/HandyOrg/HandyControl/issues/629) 仍 open。未发现可核实的关闭原因。
- 适用性：个人版仅维护 .NET 10 WPF，两个 PR 的功能与当前控件模型匹配，因此采用源码级最小移植，没有 cherry-pick 官方提交，也没有修改 Avalonia。
- 实现取舍：PropertyGrid 使用不区分大小写的顺序表，忽略空白、重复和未知名称，未配置项排在末尾；排序配置替换后刷新现有视图，保持当前按名称/按分类模式，不重建 PropertyItem 和编辑器。枚举编辑器保留完整枚举值，带 Description 显示描述，无 Description 回退字段名，并通过 SelectedValuePath 保持 TwoWay 枚举写回。CalendarWithClock 将内部时钟抽象为 ClockBase，ClockType 只接受 Clock/ListClock；切换时解绑旧事件、保留 DisplayDateTime 待确认值并更新已加载模板中的时钟。DateTimePicker 对 ClockType 建立到内嵌 CalendarWithClock 的单向绑定，复用现有 ListClock 样式。
- Demo：PropertyGrid 展示 Description 和自定义排序；CalendarWithClock、DateTimePicker 增加 ClockType 切换示例及当前已确认值展示。
- 验证：`dotnet build src/Net_GE45/HandyControlDemo_Net_GE45/HandyControlDemo_Net_GE45.csproj -c Release --no-incremental` 通过，0 警告、0 错误；`dotnet build doc/maintenance/verification/WpfSmoke/WpfSmoke.csproj -c Release --no-incremental` 通过，0 警告、0 错误；WpfSmoke 在 SkinDefault、SkinDark、SkinViolet 下均通过排序边界、枚举描述/回写/只读、编辑器实例保持、时钟切换、待确认时间、旧事件解绑、列表编辑、DateTimePicker 弹窗绑定和确认关闭。
- 主题生成阻塞修复：现有 `Styles/Base/ComboBoxBaseStyle.xaml` 第 938 行重复声明 `StaysOpen`，导致 XamlCombine 吞掉 XML 异常且返回成功，旧 Theme.xaml 被继续编译。删除重复属性后正常生成，最终 Theme.xaml 相对 HEAD 为 10 行新增、2 行删除，包含 ListClock 布局和源 ComboBox 样式既有的 StaysOpen 设置；用户对生成文件的大量格式化由生成器重新规范化。已增加最终 DLL 的 ListClock 标题和布局断言，避免仅验证控件行为而漏掉主题未更新。
- 补充验证：CalendarWithClock 重新应用模板、DateTimePicker 默认/Extend/Plus/Plus.Small 模板切换均通过；实际加载三个已编译 Demo 页面，两个时钟页面的 XAML 开关绑定通过。`git diff --check` 通过。
- 个人基线：`3252fbdf6104d8aad8d21733693c3d22e92e3082`，本次为未提交工作区构建，版本保持 `3.6.1.0`。最终主 DLL SHA256 为 `A7CE9AE1555FEE106706DA3FE575B524E190C5DF136EF18C24728904F9193170`；英文卫星 SHA256 为 `166E556ADF26C7FB9DC52761C9A92FA27D1B29A4F762508444C3A74B83747461`。HC-M003 的下游部署结果仅代表之前那批产物。
- 依据：[WPF 默认视图文档](https://learn.microsoft.com/en-us/dotnet/api/system.windows.data.collectionviewsource.getdefaultview) 说明视图承载排序分组；本次刷新现有视图保留排序模式。代码核实 `.Do` 本身已将序列物化，因此未将延迟枚举认定为缺陷，也未增加多余的 ToList。
- 未验证项：真实 Demo 的人工鼠标键盘操作、100%/150%/200% DPI、多屏弹窗边界和输入法；未升级 JuLink.Ultron 的 DLL，未执行下游回归。构建成功和自动冒烟不能替代这些检查。
- 状态：工作区已完成代码、Demo 和自动验证，尚未提交或推送；该记录与代码应作为独立补丁提交，便于回退。
- 回退：回退本条记录对应的两个 PR 代码、Demo、`.projitems` 和维护文档改动；不要覆盖同一工作区中用户既有的版本、主题和下游接入修改。

## HC-M003：发布 3.6.1.0 并接入下游英文卫星程序集

- 范围：WPF 维护版本更新为 `3.6.1.0`；删除旧语言产物；下游 `JuLink.Common.UI.Wpf` 改为直接引用主 DLL，并将英文卫星程序集配置为自动复制到输出和发布目录的 `en/` 子目录。
- 设计裁决：`HandyControl.resources.dll` 不作为普通程序集引用；.NET 通过 `en/HandyControl.resources.dll` 的文化目录自动加载。业务项目只需要主 `HandyControl.dll` 和英文卫星程序集，不需要 Demo 的 `HandyControlDemo.resources.dll`。
- 下游配置：`JuLink.Common.UI.Wpf.csproj` 使用 `Dlls\\HandyControl.dll` 的普通 Reference；使用 `Dlls\\en\\HandyControl.resources.dll` 的 `None Include`，同时设置 `CopyToOutputDirectory`、`CopyToPublishDirectory` 和 `TargetPath=en\\HandyControl.resources.dll`。
- 产物核验：HandyControl 主 DLL、英文卫星 DLL 及业务 Debug/Release/Publish 接入产物均为 `3.6.1.0`，SHA256 与库 Release 产物一致。HandyControl Demo 及引用项目 Debug/Release 构建均为 0 警告、0 错误；三皮肤、中英文切换及控件冒烟检查通过。业务首次 Debug 构建 510 警告、0 错误，后续增量构建 0 警告、0 错误；Release/Publish 成功并包含 `en/HandyControl.resources.dll`，仍报告 nullable 等警告。未建立业务修改前警告基线，不以增量构建结果代表全量无警告。
- 清理结果：库和 Demo Debug/Release 输出中的卫星程序集仅保留 `en`；业务 `Dlls` 目录删除旧的平铺 `HandyControl.resources.dll`，保留 `Dlls/en/HandyControl.resources.dll`。
- 当前状态：HandyControl 与 JuLink.Ultron 均存在本轮及用户既有未提交修改，尚未提交或推送；发布验证目录已清理。

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
