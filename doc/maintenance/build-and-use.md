# 构建与二进制接入

## 日常 WPF 构建

在 Windows 上安装 .NET 10 SDK；使用 Visual Studio 时需支持 .NET 10 并安装 .NET 桌面开发组件。以下 PowerShell 命令在仓库根目录执行：

```powershell
dotnet build src/Net_GE45/HandyControl_Net_GE45/HandyControl_Net_GE45.csproj -c Release
dotnet build src/Net_GE45/HandyControlDemo_Net_GE45/HandyControlDemo_Net_GE45.csproj -c Release
```

Demo 项目引用库及 DemoCode，会一起构建。当前仅维护 WPF，优先使用项目入口，避免整套 solution restore 把 Avalonia 的依赖也拉入验证。Visual Studio 中选择 `Release-Net-GE45` / `Debug-Net-GE45`；这些只是保留的配置名称，不表示旧框架支持。

### Visual Studio 跳过依赖库、运行结果未更新

设置 WPF Demo 为启动项目不会自动切换解决方案配置。若活动配置仍为 `Debug-Avalonia` / `Release-Avalonia`，WPF 控件库、Demo 和 DemoCode 的“生成”均未勾选；单独生成 Demo 不能据此保证两个依赖库更新。2026-09-14 已在 VS 实例中确认：启动项目为 WPF Demo，活动配置为 `Debug-Avalonia`，三个 WPF 项目的 `ShouldBuild` 均为 false。

停止调试，在“生成 → 配置管理器”选择 `Debug-Net-GE45 | Any CPU`（发布时用 `Release-Net-GE45`），确认 `HandyControl_Net_GE45`、`HandyControlDemo_Net_GE45`、`HandyControlDemo_Code` 三项均勾选“生成”，然后重新生成解决方案并启动 Demo。Avalonia 项目在 WPF 配置下不参与生成是正常行为；无需修改项目引用、强制关闭增量检查或手工逐个生成依赖库。

Demo 标题和关于窗口的版本读取入口程序集 `HandyControlDemo.dll` 的文件版本；确认控件库更新时，还应核对 Demo 输出目录中的 `HandyControl.dll` 与库输出的版本或哈希，不能仅凭标题判断。

需要完整重编译时添加 `--no-incremental`。第一次构建需要 NuGet 源可用；不要在尚未还原时使用 `--no-restore`。现行维护版本为 `3.6.9.0`，由 `src/Directory.Build.Props` 统一设置 `Version`、`FileVersion` 和 `AssemblyVersion`。

产物目录：

- 库：`src/Net_GE45/HandyControl_Net_GE45/bin/Release/net10.0-windows/`
- Demo：`src/Net_GE45/HandyControlDemo_Net_GE45/bin/Release/net10.0-windows/`

核心文件是 `HandyControl.dll`；简体中文内置其中，不产生独立中文语言包。英文包为 `en/HandyControl.resources.dll`。XML 是可选的 IDE 智能提示文档，不是运行依赖；按当前部署要求不向 JuLink.Ultron 分发 PDB。主 DLL 与英文包应使用同批构建产物。语言支持及接入细节见下文。

## 下游直接引用 DLL

下游 WPF 项目最低为 `net10.0-windows`，启用 `UseWPF`。推荐把同一批构建产物复制到下游固定的 `lib/HandyControl/` 目录，避免 HintPath 依赖维护者机器上的绝对路径。示例：

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
</PropertyGroup>
<ItemGroup>
  <Reference Include="HandyControl">
    <HintPath>lib/HandyControl/HandyControl.dll</HintPath>
    <Private>true</Private>
  </Reference>
</ItemGroup>
```

移除下游已有的同名官方 NuGet 引用，保证一次运行只解析到一份 HandyControl。构建及发布后检查实际输出目录中的 DLL 与语言子目录；按需明确配置卫星资源复制，不能仅凭源目录中存在文件认定发布包完整。

应用资源仍使用原有 URI，先皮肤后主题：

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml" />
      <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

控件命名空间：`xmlns:hc="https://handyorg.github.io/handycontrol"`。参考[官方快速开始](https://handyorg.github.io/handycontrol/quick_start/)，但框架支持政策以个人版[维护范围](scope.md)为准。

## 中英文语言包与切换

库只保留 `Lang.resx`（简体中文默认资源）和 `Lang.en.resx`（英文）。产物为：

- 中文及控件本体：`src/Net_GE45/HandyControl_Net_GE45/bin/Release/net10.0-windows/HandyControl.dll`。
- 英文：`src/Net_GE45/HandyControl_Net_GE45/bin/Release/net10.0-windows/en/HandyControl.resources.dll`。
- Demo 自身英文文案：`src/Net_GE45/HandyControlDemo_Net_GE45/bin/Release/net10.0-windows/en/HandyControlDemo.resources.dll`，业务项目不需要此文件。

将英文卫星程序集放在应用输出目录的 `en/` 子目录，不能与主 DLL 平铺。卫星程序集通过 ResourceManager 按文化自动加载，无须给它添加普通程序集 Reference。若放到项目的 `lib/HandyControl/en/`，可显式配置复制到构建和发布目录：

```xml
<ItemGroup>
  <None Include="lib/HandyControl/en/HandyControl.resources.dll"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest"
        TargetPath="en/HandyControl.resources.dll" />
</ItemGroup>
```

以上使用 `Include` 显式加入英文 DLL；如果项目已将该文件列为 `None` 项，则改用 `Update`，避免重复加入。仅使用 `Update` 不会创建原本不存在的文件项。主程序集按前文添加 Reference。

先加载 HandyControl 皮肤和主题字典，然后在 UI 线程初始化语言（建议创建首个窗口之前）：

```csharp
HandyControl.Tools.ConfigHelper.Instance.SetLang("zh-cn"); // 简体中文
HandyControl.Tools.ConfigHelper.Instance.SetLang("en");    // 英文，二选一
```

`en-US` 等英文区域会回退使用 `en` 包；没有匹配翻译时回退到内置中文。Demo 仅显示中英文按钮，旧配置中保存的其他语言在启动时归一为 `zh-cn`，英文区域归一为 `en`。

需要运行时更新的业务控件可绑定库的语言提供器：

```csharp
HandyControl.Properties.Langs.LangProvider.SetLang(
    confirmButton, System.Windows.Controls.ContentControl.ContentProperty, "Confirm");
```

再调用 `ConfigHelper.Instance.SetLang` 时此绑定会更新。`x:Static Lang.Confirm` 或一次性赋值的文案不会自动重新求值，需重新创建相关视图或改用动态语言绑定。该接口管理控件库语言，不翻译业务项目自身的文案，也不替代业务日期/数字格式策略。

升级已有部署时移除旧 HandyControl/HandyControlDemo 其他语言卫星 DLL；同目录可能含其他依赖的资源，不能直接删除其他依赖的语言文件。此次 Release 编译前执行了 clean，已确认 WPF 库输出只有英文卫星程序集。

## PropertyGrid 排序与枚举描述

`CategoryOrder` 接受分类名称序列，`PropertyOrder` 接受 CLR 属性名称序列（不是 DisplayName）。分类模式依次按分类优先级、分类名称、属性优先级、显示名称排序；未列出的项仍显示在对应优先项之后。比较忽略大小写，空白和重复配置不增加排序项，未知名称不影响实际项目之间的顺序。

```csharp
propertyGrid.CategoryOrder = new[] { "通讯参数", "运行参数" };
propertyGrid.PropertyOrder = new[] { "PortName", "BaudRate", "Timeout" };
propertyGrid.SelectedObject = settings;
```

运行时请赋予新的序列以刷新排序；不监听已有集合内部的 Add/Remove。更新优先级不会改变用户当前选择的“按名称”模式，也不会重建编辑器。按名称模式仍按 CLR 属性名称排序。

枚举成员带 `DescriptionAttribute` 时，下拉框显示描述；没有描述时显示字段名称，绑定写回值仍为枚举。只读属性对应下拉框禁用。Description 是静态元数据，不随中英文切换自动翻译。

```csharp
public enum ConnectionMode
{
    [System.ComponentModel.Description("自动连接")]
    Automatic,
    Manual
}
```

## 日期时间控件选择 Clock / ListClock

`DateTimePicker` 和 `CalendarWithClock` 均提供 `ClockType`，默认 `Clock`，可在 XAML 或运行时改为 `ListClock`：

```xml
<hc:DateTimePicker ClockType="ListClock" />
<hc:CalendarWithClock ClockType="ListClock" ShowConfirmButton="True" />
```

切换会保留 `DisplayDateTime` 中尚未确认的编辑内容；确认后才更新 `SelectedDateTime`。DateTimePicker 自动将 ClockType 传给弹窗内的 CalendarWithClock。内置模板复用现有 ListClock 样式；业务若完全替换 CalendarWithClock 模板，需要自行适配两种时钟的布局。

Demo 的 PropertyGrid 页面有排序开关及枚举描述示例；CalendarWithClock 和 DateTimePicker 页面有运行时 ClockType 开关及已确认值展示，DateTimePicker 还展示 ListClock 的 Plus/Small 样式。

## 运行时设计令牌（动态字体、尺寸与颜色）

个人库的基础样式已改为**动态消费**下列资源，下游（如 JuLink.Ultron 的框架样式系统）可以在运行时整体替换这些键而不重建控件。库自身仍保留 `Themes/Basic/` 中的原值作为独立运行默认值。

| 资源键 | 类型 | 消费点 |
| --- | --- | --- |
| `DefaultFontFamily` | `FontFamily` | `BaseStyle`（默认 Microsoft YaHei UI） |
| `TextFontSize` | `Double` | `BaseStyle` |
| `DefaultControlHeight` | `Double` | `InputElementBaseStyle`、`ButtonBaseBaseStyle`、按钮组项、ComboBox 项与 Extend、TextBox Extend、NumericUpDown Extend、AutoCompleteTextBox |
| `DefaultControlPadding` | `Thickness` | `ButtonBaseBaseStyle`、ComboBox 可编辑输入框与下拉项 |
| `DefaultIconSize` | `Double` | `ButtonBaseBaseStyle`（按钮模板中图标 `Path` 的默认方框，默认 16） |
| `DefaultInputPadding` | `Thickness` | `InputElementBaseStyle`、WatermarkTextBox、AutoCompleteTextBox |
| `DefaultCornerRadius` | `CornerRadius` | `InputElementBaseStyle`、`ButtonBaseStyle`、`CardBaseStyle`、AutoCompleteTextBox |

`ButtonBaseBaseStyle` 不再固定 `Height`，改为 `Height=Auto` + 动态 `MinHeight`，字号增大时按钮自然增高。它同时给图标 `Path` 提供默认方框（`hc:IconElement.Width`/`Height` = `DefaultIconSize`），因此 `Height=Auto` 下 `Stretch=Uniform` 的图标几何不会按 1024 级原始坐标把按钮撑开；调用方仍可用显式 `Height` 或 `hc:IconElement.Width`/`Height` 覆盖。注意 `MinHeight` 在 WPF 布局中优先于 `Height`，按钮族 `.Small` 样式必须同时给出 `MinHeight`（库内统一为 20），只写 `Height=20` 会被继承的 `MinHeight=28` 压回 28。纯图标按钮（`ButtonIcon`、`RepeatButtonIcon` 及两个圆形变体）没有文本参与定尺寸，样式内直接给出方形尺寸；`ButtonIconCircular`/`RepeatButtonIconCircular` 的宽高都用 `DynamicResource DefaultControlHeight`，运行时改该令牌仍保持 1:1。反向的一条经验：模板内联写的 `Height` 在依赖属性优先级上压过样式里的 `Height`，但压不过 `MinHeight`。数字框模板里的上下按钮是半高子按钮（各占一行，`Height="Auto"` + `VerticalAlignment="Stretch"`），必须各自显式 `MinHeight="0"`，否则会各自继承按钮基类来的 `MinHeight=DefaultControlHeight`，两行相加把数字框撑成双倍高度（实测 56 对 28）；`NumericUpDownBaseStyle.xaml` 的 5 个模板共 10 处已清零。数字框本身不设固定高度，因此放大字号时随内容增高、不被裁切。

输入类控件（TextBox、ComboBox、NumericUpDown）与 `PropertyGrid` 的样式都不自己设 `FontSize`，它们按 WPF 继承取字体；`TextFontSize` 只直接作用于以 `BaseStyle` 为基的控件（`Window`/`WindowWin10` 隐式样式也不设字号）。下游要用令牌驱动输入类或属性网格的字号，应在宿主（窗口或 App）层设置字号，而不是期待控件样式自带。实测宿主字号 24 时 TextBox、ComboBox 与三种 NumericUpDown 模板同为 30.67，默认字号时四者同为 28。

按钮组三类项（`ButtonGroupItemBaseStyle`、`RadioGroupItemBaseStyle`、`ToggleButtonGroupItemBaseStyle`）统一为 `Height=Auto` + 动态 `MinHeight`，且 `VerticalAlignment=Stretch`：按钮组按内容定高时与原来的居中效果等价，组被拉高时组内项填充该行。PropertyGrid 工具栏正依赖这两点——排序按钮是按钮组里的 `RadioButton`，必须与同一行里更高的 `SearchBar` 同高；工具栏的 `hc:ButtonGroup` 因此显式 `VerticalAlignment="Stretch"`，搜索框的 `hc:InfoElement.MinContentHeight` 也改为动态。实测 `DefaultControlHeight` ∈ {28,36,48,21,42,27,54} 与宿主字号 ∈ {12,24,9,18} 的组合下两者高度差始终 ≤ 0.5；令牌小于字号自然高度时（如令牌 28、字号 24）两者一起被内容撑到 30.67，而不是差 2.67 或被裁切。不要用固定高度去“对齐”工具栏，也不要让按钮高度单向跟随搜索框的实际高度（缩小字号时会卡住不回落）。

其余仍为 `StaticResource` 的尺寸/圆角消费点（Border、Calendar、CheckBox、RadioButton、Slider、Badge、DataGrid 等）不在本轮范围，需要时按同一方式逐项迁移。

颜色方面有两条实测结论，接入方需要遵守：

- 覆盖 `Themes/Basic/Colors/Colors*.xaml` 的 `Color` 键**不能**让 `Basic/Brushes.xaml` 中已解析的画刷在运行时变色。这些画刷虽然在首次解析时是活的 `DynamicResource`，但之后不会因新增合并字典而重新求值（`WpfSmoke` 的运行时令牌覆盖断言即针对该行为）。
- 因此运行时改配色必须由上层**提供同名实体画刷**：`PrimaryTextBrush`、`SecondaryTextBrush`、`ThirdlyTextBrush`、`TextIconBrush`、`BorderBrush`、`SecondaryBorderBrush`、`BackgroundBrush`、`RegionBrush`、`SecondaryRegionBrush`、`LightPrimaryBrush`、`DarkPrimaryBrush`、`AccentBrush`、`DarkAccentBrush` 用 `SolidColorBrush`；`PrimaryBrush` 与 `TitleBrush` 必须保持 `LinearGradientBrush`，以免破坏依赖渐变类型的动画。控件属性上的 `DynamicResource`（如 `Foreground="{DynamicResource PrimaryTextBrush}"`）会正常随字典替换更新。

## 自动运行验证

[WpfSmoke](verification/WpfSmoke/Program.cs) 是无需额外测试 NuGet 包的 STA 控制台冒烟程序，直接引用库的 **Release DLL**，模拟下游二进制接入，而不是 ProjectReference。

```powershell
dotnet build src/Net_GE45/HandyControlDemo_Net_GE45/HandyControlDemo_Net_GE45.csproj -c Release --no-incremental
if ($LASTEXITCODE -ne 0) { throw 'WPF build failed' }
dotnet run --project doc/maintenance/verification/WpfSmoke/WpfSmoke.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw 'WPF smoke failed' }
```

程序加载默认、深色、紫色皮肤，逐项断言 NumericUpDown 三种模板的画笔绑定/更新、模板重新应用、文本输入及上下界，并创建不抢焦点的屏幕外窗口验证 WindowChrome、窗口模板、Growl 创建与清除。成功返回 0，失败输出异常并返回 1。需要可创建 WPF 窗口的 Windows 用户会话。

每种皮肤还执行“运行时设计令牌覆盖”检查：在活动可视树中放一个 Button 与 TextBox，先断言 `TextFontSize`/`DefaultFontFamily`/`DefaultControlHeight`/`DefaultControlPadding`/`DefaultInputPadding` 的默认消费值与 `Height=Auto`，再追加一个合并字典覆盖这些键与 `PrimaryTextBrush`/`RegionBrush`/`PrimaryBrush`，断言字体家族、字号、最小高度、内边距、前景与背景画刷都随动且 `PrimaryBrush` 仍为 `LinearGradientBrush`；移除覆盖后断言恢复基线。

每种皮肤还执行“按钮尺寸”实际布局检查：把 1024 级大坐标几何（`DeleteGeometry`）与宽比例几何（`DownGeometry`）挂到普通“图标+文字”按钮上，断言按钮高度（≤40）与宽度（≤120）、图标框（≤20）都被 `DefaultIconSize` 限制；再断言 `ButtonIcon` 与 `ButtonIconCircular` 为 28×28（圆形 1:1）、`ButtonIconCircular.Small` 为 20×20、`ButtonDefault.Small` 恰为 20；最后运行时覆盖 `DefaultControlHeight=44`，断言文本按钮与图标按钮随动、圆形仍 1:1、Small 仍为 20，移除覆盖后恢复 28。

每种皮肤还执行“扁平化单子包装”结构检查：确认图标模板（`ButtonIcon`、`RepeatButtonIcon`、`ToggleButtonIcon`、`ToggleButtonIconTransparent`、`ToggleBlockIcon`）的图标 `Path` 直接挂在模板 `Border`（或模板根）下、不再被冗余 `ContentControl` 包裹，且 `Path.Margin` 跟随控件 `Padding`；并确认 `ImageViewer` 的 `PART_ImageMain` 直接挂在 `PART_PanelMain` 下、不再被透传 `Border` 包裹，`Margin`/对齐取自控件（尺寸与裁剪仍由上述“按钮尺寸/图标裁剪”检查覆盖）。

每种皮肤还执行“数字框高度”实际布局检查：默认字号下 `TextBox`、`ComboBox` 与三种模板的 `NumericUpDown` 必须同为 28，上下按钮 `MinHeight=0` 且不超过数字框一半；把宿主窗口字号改为 24 后四者必须同步增高到同一值（实测 30.67）且数字框既不裁切也不翻倍，恢复字号后回到 28。

每种皮肤还执行“PropertyGrid 工具栏高度”与“按钮组项尺寸”实际布局检查：前者用真实 `hc:PropertyGrid` 的工具栏，在 10 组 `DefaultControlHeight` × 宿主字号组合下断言搜索框不低于令牌、字号高于令牌时搜索框必须长于令牌（不得被裁切）、两个排序按钮与搜索框实际高度差 ≤ 0.5；后者断言同一按钮组内 RadioButton/Button/ToggleButton 三类项在令牌 28/36/48 下都实际等于令牌。

程序还验证 PropertyGrid 排序边界、枚举描述/回写/只读、排序时编辑器实例保持，以及 CalendarWithClock/DateTimePicker 时钟切换、待确认时间、旧事件解绑、列表编辑、确认关闭、模板更换和最终 DLL 的 ListClock 标题布局。

可传入已构建的 Demo DLL 路径，额外加载三个相关 Demo 页面并验证时钟开关绑定：

```powershell
dotnet run --project doc/maintenance/verification/WpfSmoke/WpfSmoke.csproj -c Release -- src/Net_GE45/HandyControlDemo_Net_GE45/bin/Release/net10.0-windows/HandyControlDemo.dll
```

该验证不替代真实界面检查。涉及相应区域的补丁还需检查：100%/150%/200% DPI、多屏最大化和任务栏、键盘/鼠标输入、中文输入法、弹出层、通知动画/关闭计时、语言切换。仅编译 Demo 不代表这些交互已验证。

## DLL 归档与回退

每批分发记录 Git SHA（未提交则明确标为工作区构建）、SDK/Runtime、构建配置、程序集版本、DLL SHA256、补丁编号及验证结果。可用 `Get-FileHash -Algorithm SHA256` 获取摘要。相同程序集版本不代表 DLL 相同，本次分发版本为 `3.6.2.0`，须通过 SHA 与哈希区分。

替换下游文件前归档当前整套库文件；回退时恢复同一批 DLL、XML、卫星资源并重建下游。不得声称已验证未提供的下游项目。

## 旧发布流水线的边界

`build/build.config.xml` 已收敛到 .NET 10 Windows。`build/build.cake` 中发布资产改为从配置枚举目录，并删除 Framework/Core 分支。Squirrel 的 `lib/net45` 是该旧打包工具的布局约定，保留不代表库支持 .NET Framework。

当前交付使用 `dotnet build`。Cake/Squirrel 安装包及公开 NuGet/GitHub 发布未验证；脚本仍含上游仓库发布目标和元数据，`publish` 会提交、打标签、推送包、创建 Release，Setup 还会清理输出并下载工具。后续确需个人版发布时须先单独配置和验证，不能把它当作普通 DLL 编译入口。

## 2026-10-08 / 3.6.10.0 分类外观资源与静态布局边界

按钮、输入、区域、卡片、弹层边框/圆角/背景/前景/描边独立资源，Separator分隔线单独消费；ButtonMinHeight/InputMinHeight/DataRowMinHeight独立。按钮组与图标按钮改用按钮高度，属性网格工具栏仍按行布局等高；数字框内部上下按钮保持MinHeight=0。RegionPadding与DataCellPadding分别用于列表外框与数据项。按钮组悬停/按下接ButtonHoverBrush/ButtonPressedBrush。卡片页眉页脚内边距采用StaticResource CardHeaderPadding/CardFooterPadding，不要求即时更新。StaticResource已在样式定义作用域解析时，下游应覆盖/派生样式或在加载前配置，不承诺替换字典更新已创建控件。

Theme.xaml由现有XamlCombine从源字典生成。Release库/Demo构建0错误、2206既有警告；WpfSmoke更新分类键并通过SkinDefault/SkinDark/SkinViolet、属性网格/Clock/DateTimePicker编译Demo和受限窗口滚动。下游Ultron369项回归通过、两示例构建0警告0错误。未声明全控件全DPI人工验收。

同批主DLL SHA256=05B58F8F7455B3264AEBDC720EECE9E475153C5ADB0AD357970681C66E43B56C，英文卫星=79916386E8DBB8A2D92DBB1F741A99AFBCBF8595C4332353DEC26F491434B6C6；XML同步。证据Ultron artifacts/style-categories-hc-build.log和style-categories-hc-smoke.log。提交及远端结果见JAX唯一交付总账。