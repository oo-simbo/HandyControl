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

需要完整重编译时添加 `--no-incremental`。第一次构建需要 NuGet 源可用；不要在尚未还原时使用 `--no-restore`。现行维护版本为 `3.6.23.0`，由 `src/Directory.Build.Props` 统一设置 `Version`、`FileVersion` 和 `AssemblyVersion`。

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

### 3.6.18：非按钮/输入控件的令牌消费扩展

按钮族与输入类之外，`Label`、`Tag`、`SplitButton`、`Pagination` 页码按钮、`GroupBox` 标题、`PinBox` 项、`Menu`/`ContextMenu` 菜单项与 `ChatBubble` 的高度/尺寸此前写死为 `StaticResource`，运行时改令牌或放大字号都不生效。3.6.18 把这些消费点改为动态令牌 + 内容定高，未新增全局参数：

| 控件 | 消费的令牌 | 变化 |
| --- | --- | --- |
| `Label`、`Tag` | `DefaultControlHeight`（`MinHeight`）、`DefaultControlPadding`、`DefaultCornerRadius` | 固定 `Height=28` 改为 `MinHeight`，字号高于令牌时随内容增高；`Height` 为 `Auto` |
| `SplitButton` | `ButtonMinHeight`（`MinHeight`）、`DefaultControlPadding`、`DefaultCornerRadius`、`DefaultIconSize`（图标框） | 固定 28 改为按钮族同一规则；补 `hc:IconElement.Width`/`Height` 默认方框，1024 级几何在 `Height=Auto` 下不再撑开按钮 |
| `Pagination` 页码按钮 | `ButtonMinHeight`（`MinHeight`） | 与左右箭头按钮、跳转数字框共用同一令牌（令牌 36/48 时不再只有页码按钮是 28） |
| `GroupBox` | `DefaultControlHeight`（`hc:TitleElement.MinHeight`/`MinWidth`） | 标题最小尺寸随令牌变化 |
| `PinBox` | `DefaultControlHeight`（`ItemWidth`/`ItemHeight`） | 项尺寸随令牌变化 |
| `Menu`、`ContextMenu` | `DefaultControlHeight`（`hc:MenuAttach.ItemMinHeight`）、`DefaultControlPadding`、`DefaultCornerRadius` | 菜单项最小高度随令牌变化 |
| `Menu.Small` | `SmallControlHeight` | 由 `DefaultControlHeight`(28) 改为 `SmallControlHeight`(20)，与 `ContextMenu.Small` 一致，`SmallControlHeight` 令牌此前无消费点 |
| `ChatBubble` | `DefaultControlHeight`（`MinHeight`） | 最小高度随令牌变化 |

Small 变体规则（沿用 HC-M010 的既有约定）：基类改为动态 `MinHeight` 后，`.Small` 只写 `Height=20` 会被继承的 `MinHeight` 压回 28，必须同时给 `MinHeight=20`。3.6.18 为 `Label*.Small`（6 个）与 `SplitButton*.Small`（6 个）补齐 `MinHeight=20`；`SplitButton*.Small` 另补 `hc:IconElement.Width=12` 与既有 `Height=12` 对齐，避免默认方框把箭头/图标缩小。方形图标控件（`ButtonIcon*`、`SplitButton`）的图标方框统一取 `DefaultIconSize`。

大字号下 `SplitButton` 与同规格普通按钮逐项等高的实测（150% DPI）：默认 `28/28`，字号 24 时 `43.33/43.33`；带图标的按钮因 1 DIP 边框在 150% 下的设备像素取整为 `28.67`，与同规格普通图标按钮一致。


颜色方面有两条实测结论，接入方需要遵守：

- 覆盖 `Themes/Basic/Colors/Colors*.xaml` 的 `Color` 键**不能**让 `Basic/Brushes.xaml` 中已解析的画刷在运行时变色。这些画刷虽然在首次解析时是活的 `DynamicResource`，但之后不会因新增合并字典而重新求值（`WpfSmoke` 的运行时令牌覆盖断言即针对该行为）。
- 因此运行时改配色必须由上层**提供同名实体画刷**：`PrimaryTextBrush`、`SecondaryTextBrush`、`ThirdlyTextBrush`、`TextIconBrush`、`BorderBrush`、`SecondaryBorderBrush`、`BackgroundBrush`、`RegionBrush`、`SecondaryRegionBrush`、`LightPrimaryBrush`、`DarkPrimaryBrush`、`AccentBrush`、`DarkAccentBrush` 用 `SolidColorBrush`；`PrimaryBrush` 与 `TitleBrush` 必须保持 `LinearGradientBrush`，以免破坏依赖渐变类型的动画。控件属性上的 `DynamicResource`（如 `Foreground="{DynamicResource PrimaryTextBrush}"`）会正常随字典替换更新。

## Card 头部标题插槽与页脚

Card 头部左右可以放图标按钮：复用既有附加属性 `hc:EdgeElement.LeftContent` / `hc:EdgeElement.RightContent`，没有新增属性、没有新增全局实时参数、没有新增转换器。

```xml
<hc:Card Header="标题">
  <hc:EdgeElement.LeftContent>
    <Button Style="{StaticResource ButtonIcon}" ToolTip="刷新" AutomationProperties.Name="刷新"
            hc:IconElement.Geometry="{StaticResource SearchGeometry}" Command="{Binding RefreshCmd}"/>
  </hc:EdgeElement.LeftContent>
  <hc:EdgeElement.RightContent>
    <StackPanel Orientation="Horizontal">
      <Button Style="{StaticResource ButtonIcon}" hc:IconElement.Geometry="{StaticResource StarGeometry}"/>
      <Button Style="{StaticResource ButtonIcon}" hc:IconElement.Geometry="{StaticResource DeleteGeometry}"/>
    </StackPanel>
  </hc:EdgeElement.RightContent>
  正文
</hc:Card>
```

模板命名结构（下游若按名取模板部件请以此为准）：

```text
PART_Header (Border, ClipToBounds=True)
└─ Grid（三列 Auto,*,Auto；Grid.IsSharedSizeScope=True，左右两列 SharedSizeGroup=CardHeaderSide，逐卡隔离）
   ├─ col0  PART_HeaderLeftContent  (ContentControl)  ← HorizontalAlignment=Left，内容为空时 Collapsed 零宽
   ├─ col1  PART_HeaderContentPanel (Border, ClipToBounds=True)
   │        └─ PART_HeaderContent (ContentPresenter)  ← 承载 Header / HeaderTemplate / HeaderTemplateSelector / HeaderStringFormat，
   │                                                    MaxWidth 绑中央面板 ActualWidth，对齐取 hc:TitleElement.HorizontalAlignment，字号/前景取 hc:TitleElement.*
   └─ col2  PART_HeaderRightContent (ContentControl)  ← HorizontalAlignment=Right，内容为空时 Collapsed 零宽
PART_Footer (Border)   ← 不再消费 SeparatorBorderThicknessTop / SeparatorColorBrush
```

契约与边界：

- 居中：左右两个 Auto 列共享 `CardHeaderSide` 恒等宽（单侧插槽时另一侧列也镜像同宽），因此**两侧插槽宽度是否相等都不影响标题位置**，标题始终相对整个 header 居中。实测（卡宽 300、`CardHeaderPadding` 16/16）无插槽 / 不等宽插槽 / 等宽插槽三种情况标题中心同为 148.67。共享尺寸的作用域是每张卡片自己的 Grid，两张插槽宽度不同的卡片互不影响（A 保持 56、B 保持 28）。
- 空插槽：每个插槽宿主 `Visibility` 绑定自身内容，为空时 `Collapsed` 且零宽，不占位、不留空。
- 可见性：`PART_Header` 仍由 `Header` 是否为空决定。`Header` 为 `null` 时整行（含插槽）折叠；需要"只有插槽、没有标题"的头部时把 `Header` 设为空字符串（非 null），此时插槽正常显示、标题不占位。若希望"只设插槽就自动显示头部"，需要新增一个布尔组合转换器，本版未加。
- 对齐：无插槽时标题按 `TitleElement.HorizontalAlignment` 贴边，`SimpleCard` 这类左对齐样式行为不变；有插槽时标题在中央列内对齐，即使设为 `Left` 也从插槽右侧开始，**不会与插槽重叠**（标题与插槽分属不同 Grid 列，几何上互不相交）。
- 窄宽：标题在中央列内按字符省略，并叠加 `PART_HeaderContentPanel` 与 `PART_Header` 的 `ClipToBounds`。宽度不足时只裁剪头部、不产生重叠：实测卡宽 160、左 1 / 右 2 不等宽插槽时两侧列 56/56、中央列 13.33，标题 `TextTrimming=CharacterEllipsis`、可见区 13.33 落在中央列内且相对整个 header 居中，`HitTest` 仍能命中插槽按钮。
- 标题省略：中央列 `ContentPresenter` 内加了一层隐式 `TextBlock` 样式 `TextTrimming=CharacterEllipsis`，不改写 `HeaderTemplate` / `HeaderTemplateSelector` / `HeaderStringFormat`；自定义模板里的 `TextBlock` 也按该隐式样式省略，非 `TextBlock` 内容由 `ClipToBounds` 兜底。
- 页脚：`PART_Footer` 去掉了上分隔线（不再消费 `SeparatorBorderThicknessTop` 与 `SeparatorColorBrush`），保留内边距、背景、前景与底部外框圆角；3.6.22起标题下分隔线也移除，不再消费分隔线资源。
- 插槽内容继承卡片前景色（不继承标题前景），按钮等自带样式的前景不受影响。

## ColorPalette 收缩与 PropertyGrid 名称列宽

`ColorPalette`（`PropertyGrid` 里颜色/画刷属性的常用色编辑器）此前在控件根上写死 `MinWidth=288`。放进窄编辑列时它会被撑到 288 并溢出，最右的“当前颜色”按钮被外层裁剪、点不到。3.6.19 去掉硬最小宽，改为可收缩布局，未新增任何全局参数：

- 根 `UserControl` 不再设 `MinWidth`；布局仍为两列 `*`（常用色 `UniformGrid`）+ `Auto`（当前颜色按钮），色板随宿主宽度收缩。
- 常用色块 `MinWidth` 16→6、`Margin` 2→1：色块可缩小但不会把色板撑出编辑列。
- 当前颜色按钮由 `MinWidth=84` 改为 `MaxWidth=64`（更短、按内容自适应），`Margin=4,2,0,2`、`Padding=5,0`，文本 `TextTrimming=CharacterEllipsis`。可见文本可被省略，完整 `#RRGGBB`/`#AARRGGBB` 仍保留在按钮 `ToolTip` 与 `AutomationProperties.Name`。
- 实测：宿主宽 160/220/320 时色板实测 160/220/320，当前颜色按钮右缘都在色板内（160 时色块区 `0..98`、按钮 `102..160`）；编译后的 `PropertyGridDemo` 两个颜色编辑器色板与编辑列同为 298、按钮右缘 298 落在槽内与视口内。
- 弹层契约不变：`CurrentColor`/`Swatches`/`HexText` 命名、`_pickerPopup` 字段、点击常用色即时写回、点击当前颜色按钮打开完整取色器并在关闭时回写颜色都保留，下游据此编写的旧测试继续可用。

`PropertyGrid` 名称列宽的库默认值同步收紧：`PropertyGridMaxTitleWidth` 由 200 降为 180（`PropertyGridMinTitleWidth` 仍 120），`PropertyGridDemo` 的显式宽由 `MinTitleWidth=200 / MaxTitleWidth=260` 改为 `120 / 180`。名称列宽算法与 `MinTitleWidth`/`MaxTitleWidth` 运行时配置机制不变。

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

每种皮肤还执行“控件令牌尺寸”检查：`Label`、`Tag`、`SplitButton`（含带 1024 级 `DeleteGeometry` 的变体）、`Pagination` 页码按钮与 `GroupBox` 标题在默认令牌下与同规格普通按钮等高（实测 28；带图标时两者同为 28.67），运行时覆盖 `ButtonMinHeight`/`DefaultControlHeight` 为 44 时同时变为 44，`LabelDefault.Small`/`SplitButtonDefault.Small` 保持 20，且 `SplitButton` 的图标框不超过 `DefaultIconSize`；把 `TextFontSize` 与宿主字号放大到 24 时 `Label`/`Tag`/`SplitButton` 随内容增高（实测 43.33/40.67/43.33，`SplitButton` 与同规格普通按钮逐项相等），移除覆盖后回到 28。反向验证：把 `LabelBaseStyle` 改回固定 `Height=StaticResource DefaultControlHeight`，冒烟在 “Label must consume the dynamic control height 44: 28” 失败；把 `SplitButtonBaseStyle` 改回固定 `Height=StaticResource DefaultControlHeight` 并去掉图标方框，冒烟在 “an icon SplitButton must match an icon button: 28 vs 28.666666666666668” 失败。

每种皮肤还执行“card header slots”检查：`PART_Header.Child` 必须是 Grid、`PART_HeaderContent` 仍是 `ContentSource=Header`；头部为三列 `Auto,* ,Auto` 且两侧列 `SharedSizeGroup=CardHeaderSide` 恒等宽；无插槽时两侧插槽 `Collapsed` 且零宽；不等宽插槽（左 1 按钮 / 右 2 按钮）与等宽插槽下标题中心一致（实测三种情况同为 148.67）且与插槽不重叠；单侧插槽时另一侧宿主折叠但其列镜像等宽；两张插槽宽度不同的卡片共享尺寸逐卡隔离（实测 A 56 / B 28）；卡宽 160 + 不等宽插槽时标题 `TextTrimming=CharacterEllipsis`、可见区落在中央列内且居中不重叠（实测两侧 56/56、中央 13.33、可见区 13.33）；插槽按钮保留 ToolTip 与 Automation 名称、可命中（`HitTest` 命中按钮本身）并触发 `Click`；`Header=null` 时整行折叠，`Header=""` 时头部与插槽可见且标题零宽；左对齐标题仍贴内边距左缘；`HeaderTemplate` 继续生效；页脚 `BorderThickness=0,0,0,0`、`BorderBrush=null`、底部圆角保留（实测 `0,0,6,6`），3.6.22起头部下分隔线移除。反向验证：去掉两侧列的 `SharedSizeGroup` → 失败于 `header side columns must share CardHeaderSide: 28 vs 56`；恢复页脚 `BorderThickness`/`BorderBrush` → 失败于 `the card footer must not draw a separator line: 0,1,0,0`。

每种皮肤还执行“color palette”检查：把 `ColorPalette` 放进 160/220/320 DIP 的窄宿主，断言色板收敛到宿主宽度（修复前实测被撑到 288）、色块可缩但不溢出、当前颜色按钮完整落在色板内且不被任何祖先布局裁切（`LayoutInformation.GetLayoutClip`）；`#80123456` 全文保留在 `HexText`、`ToolTip` 与 Automation 名称里；点击当前颜色按钮打开 `_pickerPopup`、设置 `picker.SelectedBrush` 后关闭弹层必须回写 `SelectedBrush`（下游旧测试契约）。反向验证：恢复控件根 `MinWidth=288` → 失败于 `palette width 160: the palette must shrink to its slot instead of overflowing: 288`。

传入已构建的 Demo DLL 路径时，还断言编译后 `PropertyGridDemo` 的两个颜色编辑器（`Color`/`Brush`）色板不超过编辑列、当前颜色按钮落在编辑列与 Demo 视口内（修复前实测色板 288 对编辑列 264.67）。

传入已构建的 Demo DLL 路径时，除三个时钟相关页面外还加载编译后的 `CardDemo`（冒烟不启动 Demo App，会先显式补上 Demo 的 `Locator` 资源）：断言插槽卡片存在、按钮带 ToolTip/Automation 名称、标题在带插槽时仍居中，并触发按钮 `Click` 检查页面处理器生效（实测反馈文本变为 `Header slot clicks: Search`）。

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
## 3.6.20 当前颜色按钮统一宽度与名称列最终默认值

按用户最终约定，ColorPalette.CurrentColor固定Width=68 DIP，不再按HEX字符串测量宽度；左间距4 DIP，Grid首列星号宽度，只有颜色网格适应剩余宽度。160/220/320 DIP色板对应颜色网格88/148/248 DIP，RGB及ARGB切换时按钮及网格分界保持不变。全文ToolTip/Automation与详细取色器回写契约不变。

HC资源和PropertyGridDemo名称列默认100–200 DIP；Ultron模型、渲染快照和Layout资源同步。已有显式配置保留。本节覆盖3.6.19的MaxWidth64及120–180阶段约定，不新增全局参数。
## 3.6.21 属性网格排序组外框复用输入框参数

仅PropertyGrid内部ButtonGroup新增专用模板：组外框动态绑定InputBorderThickness/InputBorderBrush/InputCornerRadius，最小高度InputMinHeight。内部两RadioButton清除描边、圆角、负边距和独立MinHeight，原命令、高亮、工具栏隐藏行为保持。外框内容层复用BorderClip裁剪，独立描边层不参与命中且置顶，防止高亮背景覆盖圆角内缘；通用ButtonGroup不改。

回归按实际外框与搜索框同高、按钮填满ItemsPresenter验证，避免150%布局取整引起的名义边框厚度相加误差。库/Demo构建0错误、2206既有警告，三皮肤完整冒烟通过；Ultron411项回归通过，包含动态四向/零边框、颜色和非对称圆角及按钮类参数隔离。

## 3.6.22 标准基线版本

本版作为当前维护分支的标准基线，附注标签v3.6.22。Card标题与正文之间不再绘制分隔线，PART_Header不消费SeparatorBorderThicknessBottom/SeparatorColorBrush；外框、背景、内边距、居中标题及两侧插槽不变。保留3.6.21排序组输入框样式联动、68 DIP当前颜色按钮、100–200 DIP名称列默认范围。目标net10.0-windows。

验证：库与Demo Release构建0错误、2206既有警告，三皮肤完整WpfSmoke通过；Ultron411/411回归通过，JuLink.Test.UI.Wpf构建0警告/0错误。主DLL SHA256 8BF09CE41E507D3C3B9F954B2559032C77771237D45863A2E51617EFDC8C7375。发布通过提交和附注标签定位源码，不运行旧Cake/Squirrel/NuGet发布流水线。
