# 修改与验证台账

## HC-M012：统一按钮组项动态尺寸并修正 PropertyGrid 工具栏排序按钮与搜索框不等高（2026-10-08）

- 现象（用户反馈）：Ultron 原生 PropertyGrid 的 2 个排序按钮与 SearchBar 不等高，而 HC 默认 Demo 等高。框架把 `DefaultControlHeight` 运行时覆盖为 36（另有 `DefaultControlPadding` 12,6、`DefaultInputPadding` 8,0）。
- 根因：按钮组三类项样式不统一。`ButtonGroupItemBaseStyle`（Button）在 HC-M009 已改为动态 `MinHeight`，但 `RadioGroupItemBaseStyle`、`ToggleButtonGroupItemBaseStyle` 仍写死 `Height = {StaticResource DefaultControlHeight}`（StaticResource 解析为 28，不随运行时覆盖）；PropertyGrid 工具栏的排序按钮正是 ButtonGroup 里的 RadioButton。搜索框 `SearchBarPlus` 的 `hc:InfoElement.MinContentHeight` 同样写死 StaticResource 28。于是令牌 36 下搜索框 36、排序按钮 28（反向验证实测 `sort button 28 must match the search bar 36`）；令牌 28 时两者都是 28，所以默认 Demo 看不出问题。
- 修复（`src/Shared/HandyControl_Shared/Themes`）：
  - `Styles/Base/RadioButtonBaseStyle.xaml`：`RadioGroupItemBaseStyle` 的静态 `Height` 改为 `MinHeight={DynamicResource DefaultControlHeight}`；`VerticalAlignment` 由 `Center` 改为 `Stretch`（按钮组按内容定高时与 Center 等价，只有组被拉高时才填充）。
  - `Styles/Base/ToggleButtonBaseStyle.xaml`：`ToggleButtonGroupItemBaseStyle` 同样改为动态 `MinHeight` + `Stretch`，与 `ButtonGroupItemBaseStyle`/`RadioGroupItemBaseStyle` 三者统一。
  - `Styles/Base/SearchBarBaseStyle.xaml`：`SearchBarExtendBaseStyle` 的 `hc:InfoElement.MinContentHeight` 由 StaticResource 改为 DynamicResource。
  - `Styles/Base/PropertyGridBaseStyle.xaml`：工具栏 `hc:ButtonGroup` 增加 `VerticalAlignment="Stretch"`，让排序按钮跟随同一行里更高的搜索框；默认等高时该对齐不产生视觉差异。
  - 未设置任何固定高度：`Height` 保持 Auto、令牌只是下限，字号高于令牌时由内容撑高，因此不是“用固定高度掩盖大字体裁切”。`Padding` 保持与 `ButtonGroupItemBaseStyle` 相同的 `10,0`，16px 排序图标由 `VerticalContentAlignment=Center` 居中，无需额外垂直内边距。
  - 未采用“PropertyGrid 局部把按钮高度绑定到 SearchBar 实际高度”：那是单向的“高跟矮”，缩小字号时会卡住不回落；本轮用共享动态令牌 + 拉伸对齐解决。
- 验证：库/Demo Release 全量重编译均 2206 警告、0 错误（与 HC-M009~M011 基线一致，无 XAML 警告）。WpfSmoke 三皮肤退出码 0：
  - 新增「PropertyGrid 工具栏高度」10 组规格（DefaultControlHeight 28/36/48/21/42/27/54 × 宿主字号 12/24/9/18），逐组断言搜索框 ≥ 令牌、字号自然高度高于令牌时必须长于令牌、两个排序按钮与搜索框实际高度差 ≤ 0.5。实测 `(28,12)=28/28`、`(36,12)=36/36`、`(36,24)=36/36`、`(48,12)=48/48`、`(48,24)=48/48`、`(28,24)=30.667/30.667`、`(21,9)=21.333`、`(42,18)=42`、`(27,12)=26.667`、`(54,12)=54`（本机 DPI 150% 且启用布局取整，故出现 2/3 的整数倍）。
  - 新增「按钮组项尺寸」检查：同一按钮组内 RadioButton/Button/ToggleButton 三类项在令牌 28/36/48 下均实际等于令牌。
  - 上述两项与并行工作流新增的「图标裁剪」检查（24 种图标样式）同批通过，完整套件三皮肤退出码 0。
- 反向验证：把 `RadioGroupItemBaseStyle` 改回 `Height={StaticResource DefaultControlHeight}` 并重建，冒烟在 `DefaultControlHeight=36 font=12: sort button 28 must match the search bar 36` 失败（退出码 1），精确复现用户报告；恢复后复验通过。
- 下游：本轮不改 Ultron 业务源码，只同步二进制；按授权更新 `test/JuLink.Test.Service/Common/NativePropertyGridStyleTests.cs`，新增 `PropertyGridToolbar_SortButtonsMatchSearchBarHeight`（DataRow 14/1、24/1、14/0.75、14/1.5、24/0.75、24/1.5），断言排序按钮 ≥ 动态令牌且与搜索框实际同高。实测 `dotnet test --filter NativePropertyGridStyleTests` **9/9 通过**（含既有 3 项）。经验：PropertyGrid 必须放进面板/树中再取模板内的 SearchBar，只对控件自身 Measure/Arrange 时视觉树里取不到。
- 并行工作流合并说明：同期的「图标按钮裁剪修复」（`ButtonIcon`/`RepeatButtonIcon` Padding=4、Small=2、圆形=4 及其冒烟检查与文档条目）未被本轮回退，已包含在本次构建与同步的二进制中——该条目此前记录“未复制下游 DLL”，本轮 3.6.7.0 一并分发。
- 分发：版本 `3.6.6.0` → `3.6.7.0`。主 DLL `30EAD4BFE45848CB0363D7B90908563DF45AD258A6932D5AE97766F522DFAFF6`；英文卫星 `01FADDE5CDC474894BCB036A51509503C01D6FE6BBDF396064ED18EF7B073DF6`；XML `F23540A8CE51E623C4EE7B7469B15946E33E6D4B68F006FAF3A61F5B4D11C420`。替换前 3.6.6.0 三件套归档 `artifacts/deployment-backups/Ultron-dlls-before-3.6.7.0-20261008-144135`（主 DLL `15F202418278E283E72D9C8EF57E2FE911A8944F1FD54973779C9EB1B85438FC`）。未分发 PDB，复制无文件占用，未停用任何进程。
- 卫星哈希结论更正：HC-M011 写的“英文卫星在源码未变下哈希仍随构建变化，说明非确定性”不准确。实测同一版本（3.6.7.0）两次构建的英文卫星哈希一致，3.6.5.0→3.6.6.0 的差异对应程序集版本变化，即卫星哈希随版本与资源内容确定，不是随机；XML 未嵌版本故跨版本不变。判版本仍以主 DLL 版本号/哈希为准。
- 仍写死 `StaticResource DefaultControlHeight` 的既有消费点（不在本轮范围）：`SplitButtonBaseStyle`（`Height`，其模板无默认图标框，直接改 Auto 有 HC-M010 的图标撑开风险，需单独评估）、`CheckComboBoxBaseStyle`、`ChatBubbleBaseStyle`、`DataGridBaseStyle`、`ExpanderBaseStyle`、`GroupBoxBaseStyle`、`LabelBaseStyle`、`MenuBaseStyle`、`PasswordBoxBaseStyle`、`PinBoxBaseStyle`、`TabControlBaseStyle`、`TagBaseStyle`、`TimePicker/DatePicker/DateTimePickerBaseStyle`、`TransferBaseStyle`、`TreeViewBaseStyle`、`ListView`、`Pagination`、`PropertyGrid.xaml`、`Ribbon` 等；需要时按同一方式逐项迁移。
- 源码基线：`main` @ `42a6cc5`（`refactor(property-grid): remove business attributes from HandyControl`）；本轮为**未提交工作区构建**，HC-M010/HC-M011 及更早的既有未提交改动（含 `DialogExtension.cs`）全部保留，未提交、未推送。
- 未验证项：JuLink.Ultron 全量构建与业务界面人工验收（由主代理统一构建）；100%/150%/200% DPI 切换、真实宿主鼠标键盘交互；Ultron 其他输出目录（`Release/`、`Debug/`、`src/*/bin`）仍是旧 DLL。
- 状态：源码修改、库/Demo 构建、三皮肤自动冒烟、根因反向验证、下游二进制同步与目标测试类回归全部完成；仓库未提交。日志：`artifacts/hc-3.6.7-build.log`、`artifacts/hc-3.6.7-demo-build.log`、`artifacts/hc-3.6.7-smoke.log`、`artifacts/ultron-native-grid-3.6.7.log`。
- 注意：本轮构建后若并行工作流继续修改 XAML 样式，需重新构建并重新同步 DLL 才会生效。

## 图标按钮裁剪修复（2026-10-08）

- 原因：图标按钮固定为 28/20 DIP 后，继承的文字按钮横向内边距及圆形按钮的 6 DIP 内边距挤占图标空间，Path 发生布局裁剪；仅核验控件宽高无法发现该问题。
- 修复：ButtonIcon / RepeatButtonIcon 使用 Padding=4；对应 Small 使用 Padding=2；ButtonIconCircular / RepeatButtonIconCircular 使用 Padding=4。保留其余现有尺寸、几何与样式改动。
- Demo 侧栏 ButtonShiftOut / ButtonShiftIn 的局部 Padding 从 `8 8 0 8` 改为 `8 4 0 4`，保留横向偏移和半圆把手布局，使 16 DIP 箭头有足够高度。
- 回归：WpfSmoke 新增 24 种图标样式的 Path 及祖先实际裁剪检查，覆盖普通/Small、启用/禁用、切换选中状态。修复前 ButtonIcon 的图标框 11.33×16 被裁至 5.33×15.33，检查失败；修复后当时完整冒烟在 SkinDefault/SkinDark/SkinViolet 下全部通过。
- 最终验证：正式 Demo Release 增量构建 0 警告、0 错误；实际启动正式 Demo，在当前 150% 缩放桌面确认侧栏左右箭头完整且展开/收起有效。最终复跑时图标检查仍通过，但同期新增的 PropertyGrid 工具栏检查在 DefaultControlHeight=36 时报告排序按钮 28、搜索框 36，导致完整套件退出码 1；该并行修改未由本次修复调整。日志位于 artifacts/icon-clipping-before.log、artifacts/icon-clipping-after.log、artifacts/icon-clipping-build.log。
- 交付状态：本地未提交工作区；未推送、未复制下游 DLL。下一步由工具栏修改任务解决上述高度检查失败。

## HC-M011：修正 NumericUpDown 内嵌上下按钮撑高数字框（2026-10-08）

- 现象（用户反馈）：数字框（NumericUpDown）高度是普通输入框的两倍。上一轮 HC-M009 给 `ButtonBaseBaseStyle` 加的动态 `MinHeight = DefaultControlHeight` 通过 `RepeatButtonIcon` 继承到了数字框模板里的上下按钮。
- 根因：`NumericUpDownBaseStyle.xaml` 的 5 个模板里，`UpButton`/`DownButton` 是半高子按钮（各自占一行，`Height="Auto"` + `VerticalAlignment="Stretch"`）。模板内联写的 `Height="Auto"` 按依赖属性优先级压过了样式里的 `Height`，但**压不过 `MinHeight`**：两个 `RepeatButtonIcon` 各保住一份 `MinHeight=28`，两行相加把数字框撑到 56。反向验证实测：去掉清零后 `NumericUpDown=56 vs TextBox=28`。
- 修复：在 5 个模板共 10 处 `UpButton`/`DownButton` 上显式 `MinHeight="0"`（`Styles/Base/NumericUpDownBaseStyle.xaml`），并在文件内加注释说明为什么必须逐个清零。未给数字框本身设固定高度，也没有改动其余属性，因此 24 号字时数字框仍随内容增高、不被裁切。
- PropertyGrid 原生样式核查（按要求只核查、不改外观）：
  - 分组容器 `PropertyGroupItemBaseStyle` 已是 `Expander`（Header 绑定分组名、`IsExpanded=True`）+ 内容 `Border BorderThickness="1,0,1,1" CornerRadius="0,0,4,4"`，上缘由 Expander 头、下缘圆角由该 Border 分工，切分正确，未改模板。
  - `Themes/Styles` 全目录（含 `Styles/Base`）没有任何 `FontSize="{StaticResource ...}"` 或 `CornerRadius="{StaticResource ...}"`（检索 0 命中），PropertyGrid 相关文件也未设 `FontSize`，即不存在“静态资源挡住动态字号”的点，故未做任何 Static→Dynamic 改写。
  - 实测结论：`PropertyGridBaseStyle`、`PropertyItemBaseStyle`、以及 `Window`/`WindowWin10` 隐式样式都不设 `FontSize`，所以 PropertyGrid 与 TextBox/ComboBox/NumericUpDown 一样**按继承取字体**；`TextFontSize` 只直接作用于以 `BaseStyle` 为基的控件。下游若要用令牌驱动输入类/PropertyGrid 字号，需在宿主（窗口或 App）层给字号，而不是依赖控件样式。
  - 已知但本轮未改（超出「静态资源挡动态字号」范围，改动会影响全应用或超出 PropertyGrid 边界）：`ToolTipBaseStyle` 硬编码 `FontSize="12"`（PropertyItem 描述气泡用它）；`PropertyGrid.xaml` 的 `ComboBoxItemCapsuleSingle` 用 `{StaticResource DefaultCornerRadius}`（是圆角不是字号）。需要时各改一行即可，本轮保持原外观。
- 验证：库与 Demo Release 全量重编译（`--no-incremental`）均 2206 警告、0 错误（与 HC-M009/HC-M010 基线一致，无新增、无 XAML 警告）。WpfSmoke 三皮肤全部通过（退出码 0），新增「数字框高度」检查：默认字号 `TextBox=28`、`ComboBox=28`、三种模板的 NumericUpDown 均 `28`；上下按钮 `MinHeight=0` 且实际高度不超过数字框一半；把宿主窗口字号改为 24 后四者同时为 `30.666666666666668`，数字框既未裁切也未翻倍；恢复字号后回到 28，且数字框始终不超过 TextBox 的 1.5 倍。
- 反向验证：临时去掉 10 处 `MinHeight="0"` 重新构建，冒烟在 `default font: NumericUpDown must match the TextBox height: 56 vs 28` 失败（退出码 1），确认该断言正是覆盖本次根因；恢复后复验通过。
- 分发：版本由 `3.6.5.0` 升为 `3.6.6.0`（沿用每轮 `X.Y.Z.0` 递增，便于按二进制版本追溯）。主 DLL、英文卫星、XML 同批复制到 `D:/Sourcecode/JuLink.Ultron/src/JuLink.Common.UI.Wpf/Dlls`（含 `en/`），逐文件 SHA256 与库 Release 输出一致，不分发 PDB。主 DLL `15F202418278E283E72D9C8EF57E2FE911A8944F1FD54973779C9EB1B85438FC`；英文卫星 `D65B12AC0396472F5F1B794F1E1B379C9012A839397160CE111C08E94C30C386`；XML `F23540A8CE51E623C4EE7B7469B15946E33E6D4B68F006FAF3A61F5B4D11C420`（无 API/注释变化，与 3.6.4.0/3.6.5.0 相同）。英文卫星哈希随程序集版本变化（同版本重复构建一致），判版本须用主 DLL 版本号或哈希。替换前的 3.6.5.0 三件套先归档到 `artifacts/deployment-backups/Ultron-dlls-before-3.6.6.0-20261008-140437`（主 DLL SHA256 `7696E664...`，与 HC-M010 记录一致），复制过程无文件占用，未停用任何用户进程。
- 源码基线：`main` @ `42a6cc5`；本轮为**未提交工作区构建**，保留 HC-M010 及更早的既有未提交改动（含 `DialogExtension.cs`），未回退、未提交、未推送。
- 未验证项：JuLink.Ultron 侧下游编译、属性网格原生样式替换后的人工界面验收、排序/搜索交互（本轮只改 HC 库并同步二进制）；100%/150%/200% DPI、真实宿主鼠标键盘与输入法；Ultron 其他输出目录（`Release/`、`Debug/`、`src/*/bin`）仍是旧 DLL，需其重建后生效。
- 状态：源码修改、库/Demo 构建、三皮肤冒烟、根因反向验证与下游二进制同步已完成；仓库未提交。日志：`artifacts/hc-3.6.6-build.log`、`artifacts/hc-3.6.6-demo-build.log`、`artifacts/hc-3.6.6-smoke.log`。

## HC-M010：修正图标几何撑开按钮与 Small 按钮被 MinHeight 覆盖（2026-10-08）

- 现象（下游反馈）：图标按钮尺寸巨大。上一轮 HC-M009 把 `ButtonBaseBaseStyle` 从固定 `Height` 改为 `Height=Auto` + 动态 `MinHeight` 后，按钮模板里 `Stretch=Uniform` 的图标 `Path` 失去了可依附的有限高度。
- 根因：模板中图标 `Path` 的 `Width`/`Height` 绑定 `hc:IconElement.Width`/`Height`，两者默认都是 `double.NaN`。`Height` 固定时该 Path 被按钮高度夹住并按比例缩放；改成 `Height=Auto` 后可用高度变为无限，Path 退化为按几何原始坐标测量。HandyControl 的几何是 1024 级大坐标（如 `DeleteGeometry`），因此把按钮直接撑开——实测“图标+文字”按钮被撑到 **781.33px** 高。第二个缺陷：`.Small` 样式显式 `Height=20` 时，继承来的 `MinHeight=28` 在 WPF 布局中优先于 `Height`，导致所有 Small 按钮实际仍是 28 高，圆形小按钮的 `Width=20` 也失去 1:1。
- 修复（`src/Shared/HandyControl_Shared/Themes`）：
  - `Basic/Sizes.xaml` 新增设计令牌 `DefaultIconSize`（16）。
  - `Styles/Base/ButtonBaseBaseStyle.xaml` 新增 `hc:IconElement.Width`/`Height = {DynamicResource DefaultIconSize}`：图标 Path 始终有有限方框，大坐标几何不再参与 `Height=Auto` 下的定尺寸，且调用方仍可逐个覆盖。文本按钮继续保留 `Height=Auto` + 动态 `MinHeight`，随字号增高。
  - `Styles/Button.xaml`、`Styles/RepeatButton.xaml`：纯图标按钮 `ButtonIcon`/`RepeatButtonIcon` 显式给方形尺寸（`Width`/`Height` = `DefaultControlHeight`）；`ButtonIconCircular`/`RepeatButtonIconCircular` 的 `Width` 由 `StaticResource` 改为 `DynamicResource` 并显式补 `Height`，保证运行时改 `DefaultControlHeight` 时仍为 1:1。
  - `Styles/Button.xaml`、`Styles/RepeatButton.xaml`、`Styles/ToggleButton.xaml`、`Styles/RadioButton.xaml`：41 个继承 `ButtonBaseBaseStyle` 的 `.Small` 样式补 `MinHeight=20`（Button 14、RepeatButton 14、ToggleButton 普通色 6、RadioButton 7，含 `ButtonIcon.Small`/`ButtonIconCircular.Small`/`RepeatButtonIcon*.Small`/`RadioButtonIcon.Small`），并把 `hc:IconElement.Width` 与既有 `Height=12` 对齐，保持小按钮图标框尺寸不变。
  - 边界：`ToggleButtonIcon*`、`ToggleButtonSwitch*`、`ToggleButtonFlip*`、`SplitButton*`、`RadioGroupItem*` 的基类不继承 `ButtonBaseBaseStyle`（或仍固定 `Height`），本轮无回归，也未做无谓修改。Avalonia 未改动。
- 验证：库 Release 全量重编译（`--no-incremental`）2206 警告、0 错误；Demo Release 全量重编译 2206 警告、0 错误（与 HC-M009 基线一致，无新增、无 XAML 编译警告）。WpfSmoke 在 SkinDefault/SkinDark/SkinViolet 三种皮肤下全部通过（退出码 0），新增“按钮尺寸（实际布局）”检查：1024 级 `DeleteGeometry` 与宽比例 `DownGeometry` 的“图标+文字”按钮高度 ≤40、宽度 ≤120、图标框 ≤20；`ButtonIcon` 28×28；`ButtonIconCircular` 28×28（1:1）；`ButtonIconCircular.Small` 20×20；`ButtonDefault.Small` 恰好 20；运行时追加 `DefaultControlHeight=44` 覆盖后文本按钮与图标按钮随动为 44、圆形仍 1:1、Small 保持 20，移除覆盖后恢复 28。
- 回归测试有效性反向验证：临时注释掉 `ButtonBaseBaseStyle` 的两条图标框 Setter 并重新构建，同一冒烟在 `icon+text button must keep the control height instead of the geometry: 781.3333333333334` 处失败（退出码 1），证明新增断言确实覆盖本次根因；随后恢复并复验通过。
- 分发：主 DLL、英文卫星、XML 同批复制到 `D:/Sourcecode/JuLink.Ultron/src/JuLink.Common.UI.Wpf/Dlls`（含 `en/` 子目录），逐文件 SHA256 与库 Release 输出一致，不分发 PDB。版本由 `3.6.4.0` 升为 `3.6.5.0`。主 DLL `7696E6645973C1550C8DB63248205862B01DA9F225EE84F3025F1989C6F0FEFF`；英文卫星 `6E155C672568DCA7C5538AE95899761840C4C9035290CEFD6872BB92EE3EAD31`；XML `F23540A8CE51E623C4EE7B7469B15946E33E6D4B68F006FAF3A61F5B4D11C420`（本轮无 API 与文档注释变化，与 3.6.4.0 的 XML 哈希相同，不能只凭 XML 判版本）。同一源码的增量构建与全量重编译主 DLL 哈希不同，故以最终全量重编译产物为准并重新同步。
- 回退：替换前的 3.6.4.0 三件套从 `JuLink.Ultron/Release` 回收并归档到 `artifacts/deployment-backups/Ultron-dlls-before-3.6.5.0-20261008-135528`（主 DLL 3.6.4.0、SHA256 `444DAC51527C2C06FAC70853E02494C1DECE7743449EF927057047787240149D`，与 HC-M009 记录一致）。未停用或结束任何用户进程，复制过程未遇到目标文件占用。
- 源码基线：`main` @ `42a6cc5`（`refactor(property-grid): remove business attributes from HandyControl`）；本轮为**未提交工作区构建**，保留了工作区既有未提交改动（含 `DialogExtension.cs`），未回退、未提交、未推送。
- 未验证项：JuLink.Ultron 侧的下游编译、回归与人工界面验收（按要求本轮只构建 HandyControl 仓）；100%/150%/200% DPI、真实宿主鼠标键盘交互、输入法与多屏弹窗边界；Ultron 其他输出目录（`Release/`、`Debug/`、`src/*/bin`）仍是旧 DLL，需其自行重建后生效。
- 状态：源码修改、库/Demo 构建、三皮肤自动冒烟、根因反向验证与下游二进制同步已完成；仓库未提交。日志：`artifacts/hc-3.6.5-build.log`、`artifacts/hc-3.6.5-demo-build.log`、`artifacts/hc-3.6.5-smoke.log`。

## HC-M009：基础样式动态设计令牌与 3.6.4.0 二进制交付（2026-10-08）

- 范围：仅为下游框架样式系统（JuLink.Ultron `JuLink.Common.UI.Wpf`）打通运行时字体/尺寸/颜色的动态消费；不改控件 API、不新增模板属性、不引入 JuLink 依赖。版本 `Version`/`FileVersion`/`AssemblyVersion` 由 `3.6.3.0` 升为 `3.6.4.0`。工作区中已存在的 `DialogExtension.cs` BOM 调整由用户此前会话产生，不计入本轮改动，也未回退。
- 字体：`Themes/Basic/Fonts.xaml` 新增 `DefaultFontFamily`（默认 `Microsoft YaHei UI`）；`BaseStyle` 的 `FontSize` 由 `TextFontSize` 静态改为动态，并新增动态 `FontFamily`。
- 尺寸：`BaseStyle` 的 `InputElementBaseStyle`（`CornerRadius`/`MinHeight`/`Padding`）改为动态；`ButtonBaseBaseStyle` 去掉固定 `Height`，改为 `Height=Auto` + 动态 `MinHeight`，`Padding` 改为动态；`ButtonBaseStyle` 的 `CornerRadius`、`ButtonGroupItemBaseStyle` 的 `Height`→动态 `MinHeight`、`CardBaseStyle` 的 `CornerRadius` 改为动态；`ComboBoxBaseStyle` 的可编辑输入框 `Padding`、下拉项 `Padding`/`MinHeight`、Extend `MinContentHeight`；`TextBoxBaseStyle` 的 Watermark `Padding` 与 Extend `MinContentHeight`；`NumericUpDownBaseStyle` 的 Extend `MinContentHeight`；`AutoCompleteTextBoxBaseStyle` 的 `CornerRadius`/`MinHeight`/`Padding` 与下拉项 `Padding`/`MinHeight`。其余仍为 `StaticResource` 的消费点（Border、Calendar、CheckBox、RadioButton、Slider、Badge、DataGrid 等）按“不泛改全部”保留，已在 `build-and-use.md` 列出。
- 颜色机制实测结论（重要）：`Basic/Brushes.xaml` 的画刷颜色虽是 `DynamicResource`，但在首次解析后不会因新增合并字典而重新求值，画刷本身也非冻结（`IsFrozen=False`）。因此仅覆盖 `Themes/Basic/Colors/Colors*.xaml` 的 `Color` 键无法实现运行时换色；运行时改配色必须由上层提供同名实体画刷，`PrimaryBrush`/`TitleBrush` 保持 `LinearGradientBrush` 以兼容依赖渐变类型的动画。该结论已写入维护文档，并作为下游 `WpfStyleManager` 的实现依据。
- 验证：库 Release 全量构建 2206 警告、0 错误（与 HC-M007 基线相同，无新增警告）；Demo Release 全量构建 2206 警告、0 错误。WpfSmoke 在 SkinDefault/SkinDark/SkinViolet 三种皮肤下全部通过（退出码 0），除原有 PropertyGrid/时钟/NumericUpDown/WindowChrome/Growl/语言检查外，新增“运行时设计令牌覆盖”检查：默认消费值（`TextFontSize`=12、`DefaultFontFamily`=Microsoft YaHei UI、`MinHeight`=28、`Padding`=10,5 与 8,0、`Height=Auto`）→ 追加覆盖字典后字体家族/字号/最小高度/内边距/前景/背景画刷全部随动且 `PrimaryBrush` 仍为 `LinearGradientBrush` → 移除后恢复基线；三个已编译 Demo 页面加载与绑定通过。
- 分发：主 DLL、英文卫星、XML 同批复制到 `D:/Sourcecode/JuLink.Ultron/src/JuLink.Common.UI.Wpf/Dlls`（含 `en/` 子目录），逐文件 SHA256 与库 Release 输出一致，不分发 PDB。主 DLL `444DAC51527C2C06FAC70853E02494C1DECE7743449EF927057047787240149D`；英文卫星 `385C8F4AC036CA38F84B4D3FF00F8837612C8F5EAD6577764F523A0FCB501171`；XML `F23540A8CE51E623C4EE7B7469B15946E33E6D4B68F006FAF3A61F5B4D11C420`。替换前整套旧文件备份于 `artifacts/deployment-backups/Ultron-before-3.6.4.0-20261008-121243`（旧主 DLL 3.6.3.0，SHA256 `7606E6D004C9710A7D35F2A7FF784EAC07FAB6A8CCE3C8020B14327D560D5204`）。日志：`artifacts/hc-3.6.4-build.log`、`hc-3.6.4-demo-build.log`、`hc-3.6.4-smoke.log`。
- 源码基线：`main` @ `42a6cc5`（`refactor(property-grid): remove business attributes from HandyControl`）；本轮为**未提交工作区构建**，DLL 来自含 `DialogExtension.cs` 既有改动的工作区。
- 未验证项：JuLink.Ultron 侧的下游编译、回归与人工界面验收（本轮按要求只构建 HandyControl 仓）；100%/150%/200% DPI、真实宿主鼠标键盘交互、输入法与多屏弹窗边界；仍为 `StaticResource` 的尺寸消费点不在本轮生效范围。
- 状态：已完成源码修改、库/Demo 构建、三皮肤自动冒烟与下游二进制同步；仓库尚未提交或推送。

## HC-M008：修正 Visual Studio WPF Demo 活动生成配置（2026-09-14）

- 现象：生成或运行 WPF Demo 时跳过两个依赖库，需要分别生成库才能得到最新结果。
- 实测原因：通过运行中 VS 的 DTE 读取到活动配置为 `Debug-Avalonia`，启动项目却为 `HandyControlDemo_Net_GE45`；已加载的 WPF 库、Demo、DemoCode 三个项目 `ShouldBuild` 均为 false。磁盘上的 `Debug-Net-GE45` 映射和 Demo 的两个 ProjectReference 正常，未发现应修改的依赖声明。
- 修复：停止该 Demo 的调试，将当前 VS 解决方案配置切换为 `Debug-Net-GE45` 并保存解决方案；三个 WPF 项目的 `ShouldBuild` 均变为 true。执行 VS Clean + Build，再由 VS 启动 Demo。未修改其他 VS 实例、Avalonia 配置或业务代码。
- 验证：VS BuildState=3（完成）、LastBuildInfo=0（无失败项目）；三个 Debug DLL 均重新生成且文件版本为 3.6.3.0；Demo 目录的 HandyControl.dll、HandyControlDemoCode.dll 与各自库输出 SHA256 一致。重启后进程实际加载的 HandyControlDemo.dll、HandyControl.dll 均为 3.6.3.0。未取得完整 VS 警告日志，不声明零警告。
- 边界：这是当前 VS 会话的配置修复，仓库提交仅记录排查依据和操作说明；未改写 .suo、未测试关闭再打开 VS 后的配置恢复。Demo 标题读取入口程序集文件版本，不是控件库版本。未做人工页面交互或下游二进制分发。

## HC-M007：移除新增 PropertyGrid Attribute（2026-09-14）

- 按用户要求移除 HandyControl.Data 下新增的七个元数据 Attribute 及 DateTimePickType，删除对应共享项目编译项。版本维持 3.6.3.0，nullable 和 PropertyResolver 外部特性解析、混合排序逻辑保持。
- 冒烟模型改用独立 SmokeFixtures 测试类型，不进入运行库；增加二进制断言确认八个类型均不存在。Ultron OpenDirectoryPropertyAttribute 恢复至 Common.Model.Attributes，DirectoryPropertyEditor 同步使用模型层类型。
- 验证：库/Demo Release 全量构建 2206 警告、0 错误；SkinDefault/SkinDark/SkinViolet 冒烟及三个编译后 Demo 页面全部通过；Ultron Common.UI.Wpf Release 全量构建 509 警告、0 错误，PropertyGridCompatibilityTests 3/3 通过。
- 同步主 DLL、XML、en 卫星到 D:/Sourcecode/JuLink.Ultron/src/JuLink.Common.UI.Wpf/Dlls，逐文件 SHA256 与构建源一致；Ultron Release 主 DLL/en 亦一致，无 PDB 分发。主 DLL SHA256：7606E6D004C9710A7D35F2A7FF784EAC07FAB6A8CCE3C8020B14327D560D5204。
- 旧包备份：artifacts/deployment-backups/Ultron-before-remove-attributes-20260914-172046。日志：artifacts/hc-remove-attributes-build.log、hc-remove-attributes-smoke.log、ultron-remove-attributes-build.log、ultron-remove-attributes-tests.log。
- 未执行完整解决方案、Debug 输出更新、人工 UI/DPI 或真实设备验证。提交前再次运行三皮肤/三个 Demo 冒烟和 Ultron 兼容回归，全部通过；代码与本条记录一并提交，远程交付状态记录于 JAX 总台账。

## HC-M006：3.6.3.0 混合排序与 Ultron 兼容修复（2026-09-14）

- 范围：Version/FileVersion/AssemblyVersion 升级为 3.6.3.0；WPF 主项目启用 nullable 分析；纳入 HandyControl.Data 的七个 PropertyGrid 元数据特性。启用 nullable 不代表已清理全库空值警告。
- 排序契约：PropertyResolver 通过特性名称和公开 Order 读取 JuLink 原有元数据，无 JuLink 二进制依赖；支持整数、补零字符串及可转换值，缺失、非法字符串和溢出回退 int.MaxValue。PropertyItem 保存默认排序，外部 CategoryOrder/PropertyOrder 仅覆盖匹配项，清空恢复模型排序；按 CLR 名称排序模式和编辑器实例保持不变。外部数字索引与模型优先级合并比较，不承诺全部外部项必然排在负数模型优先级之前。
- 下游修复：Ultron ConfigPropertyGrid 不再向分类/显示名添加数字前缀，改用解析器和默认/有效排序字段；日期、数字格式、文件选择编辑器恢复读取 JuLink.Common.Model.Attributes，避免同名 HandyControl 特性造成静默失效。保留用户已做的 OpenDirectoryPropertyAttribute 迁移及 ErrorCodeAttribute 文件更名；旧 DirectoryPropertyEditor 的注释掉对话框逻辑未在本轮实现。
- 验证：HandyControl 库/Demo Release 全量构建 2214 警告、0 错误；三皮肤控件 Smoke 和三个编译后 Demo 页面通过，包括非法排序字符串、数值溢出和默认回退。Ultron Common.UI.Wpf Release 全量构建 509 警告、0 错误；WPF Demo Debug 全量构建 509 警告、0 错误，最终 DLL 更新后的增量构建 238 警告、0 错误。
- 回归：Ultron 新增 PropertyGridCompatibilityTests，使用真实 JuLink 特性验证排序覆盖/清空/名称/编辑器身份及日期模式、数字格式、文件筛选配置，3/3 通过。临时撤销默认排序赋值并切回错误命名空间后 2 失败、1 通过；恢复修复后 3/3 通过。
- 分发：主 DLL、XML、en 卫星同步到 D:/Sourcecode/JuLink.Ultron/src/JuLink.Common.UI.Wpf/Dlls；不分发 PDB。旧版本备份 D:/Develop/HandyControl/artifacts/deployment-backups/Ultron-before-3.6.3.0-20260914-165639。回退需整批恢复 DLL/en 并配套回退依赖新版排序 API 的 Ultron 修改后重建。
- 最终主 DLL SHA256：49E6FFA95EBD211183B9D91353566BDD7B8F80B61CAA4A19B459A9CF24CFCB62；英文卫星：1D05652A46BB92634C1F9862C9985410B3C954C78A694BA1874E1F56A22E3D8E；XML：07707260D02BC71A7F3CB4B89EB6546B54442435438B3796A9BF4883C00AAA28。
- 日志：artifacts/hc-3.6.3-build.log、hc-3.6.3-smoke.log、ultron-hc-3.6.3-release.log、ultron-hc-3.6.3-debug.log、ultron-hc-3.6.3-debug-final.log、ultron-hc-3.6.3-tests.log、ultron-hc-3.6.3-regression-negative.log。
- 审查限制：独立 code-review 子代理调用返回 403 Insufficient account balance，未完成独立审查；主代理完成差异复核和回归验证。未运行完整 Ultron/JAX 解决方案、业务人工交互、多 DPI、真实设备或历史图形测试。
- 交付：本地 main 提交，未推送远程或创建公开 Release；提交号见 Git 历史及 JAX 总台账。

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

## 2026-10-08 / 3.6.8.0 颜色编辑、方向边框与滚动交付

纳入当前工作区全部授权改动：ColorPalette常用颜色及完整取色弹层、ColorPropertyEditor识别Color/Brush/SolidColorBrush、属性网格及Clock滚动调整、Demo颜色字段和展示、基础控件按动态方向边框资源消费；版本由Directory.Build.Props统一为3.6.8.0。Theme.xaml由现有XamlCombine生成，较大文本差异包含生成器排版变化；未修改生成器。

全量Release库/Demo构建0错误、2206条既有警告；WpfSmoke三皮肤及编译后PropertyGridDemo/CalendarWithClockDemo/DateTimePickerDemo全部通过，并验证受限窗口内属性网格能滚至底部。下游Ultron同步主DLL/英文卫星/XML，347项服务回归通过（包括ColorPalette、边框/圆角、主题与属性网格）。未声称所有DPI及所有控件人工验收。

主DLL SHA256 `6376E1E3CDA32B76937EA4C4CA868BCB2ACA09F4AE8A4811B378B1E06DC8AC88`；英文卫星 `665008CB93A6ADDEE92E66CB7909456BFA36EEFB0C264D4B88D9A2556DE8D48D`。验证日志在Ultron artifacts/theme-hc-final-build.log及theme-hc-smoke.log。提交/远端状态由JuLink.JAX交付总账记录。回退需同步回退库、卫星及下游引用。
