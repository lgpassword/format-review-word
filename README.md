# Word 格式检查系统

[English](README.en.md)

WordFormatAnalyzer 是一个本地运行的 ASP.NET Core Razor Pages Web 应用，用于根据 Word 模板或用户填写的模块规则检查文档格式。系统会分析模板、比对待检查文档，并输出页面报告、带批注 Word、HTML 报告和自动调整后的正常文档。

## 项目目标

本项目面向论文、报告、合同、方案书、投标文件等需要统一排版规范的 Word 文档审查场景。用户可以先上传模板文档，系统从模板中提取批注、段落格式、字体字号、表格结构、页面设置等信息；随后上传实际文档，系统根据模板规则生成格式问题报告。

系统也支持“用户自定义模块规则检查”：先上传需要检查的文档，系统将文档拆分为封面、目录、摘要、正文、参考文献、附录等模块，再由用户为不同模块填写检查要求并执行检查。

## 主要功能

- 支持 `.docx` 文件分析。
- 支持 `.doc` 上传；在 Windows 且安装 Microsoft Word 的环境中会转换为 `.docx` 后再分析。
- 模板优先流程：先分析模板，再上传实际文档。
- 用户自定义规则流程：先拆分文档模块，再按用户填写规则检查。
- 读取模板批注、段落、文本运行、表格和页面设置。
- 识别常见中文字体、西文字体、字号、对齐、段前段后、行距、首行缩进等规则。
- 支持上传合格参考模板，并对总模板和参考模板之间的规则差异进行冲突选择。
- 按区域和模块生成可编辑的本次检查规则。
- 检查字体字号、段落、页面设置、表格结构、大空白、标点和未完全检查项。
- 在页面报告中提供严重级别和问题类型过滤。
- 生成带 Word 批注的 `.docx`。
- 导出离线 HTML 检测报告。
- 生成按规则调整后的正常 Word 文档。
- 使用 SQLite 保存分析会话、规则选择、报告和生成文件路径。

## 技术栈

- .NET 8
- ASP.NET Core Razor Pages
- DocumentFormat.OpenXml 3.1.1
- Microsoft.Data.Sqlite 8.0.11
- Bootstrap / jQuery 静态前端资源
- Windows Word 自动化用于旧版 `.doc` 转换

## 快速开始

环境要求：

- 安装 .NET 8 SDK。
- 如需处理 `.doc` 文件，需要在 Windows 环境安装 Microsoft Word。
- `.docx` 分析不依赖本机安装 Word。

运行方式：

```bash
dotnet restore
dotnet run --urls "http://localhost:5088"
```

也可以在 Windows 上双击运行：

```bat
start-web.bat
```

启动后访问：

```text
http://localhost:5088
```

## 使用流程

模板检查流程：

1. 打开首页。
2. 在“入口一：按模板检查”上传模板 `.docx` 或 `.doc`。
3. 系统分析模板并展示模板摘要。
4. 可选：上传一个或多个合格参考模板。
5. 如模板之间存在规则冲突，在页面中选择本次采用的规则。
6. 检查并编辑本次采用的格式规则。
7. 上传实际文档。
8. 查看检测报告。
9. 下载带批注 Word、HTML 报告或正常文档。

自定义规则流程：

1. 在“入口二：用户自定义模块规则检查”上传需要检查的文档。
2. 系统拆分文档模块。
3. 在规则表中为各模块填写字体、字号、对齐、间距、行距、缩进等要求。
4. 执行检查。
5. 查看报告并下载生成结果。

## 目录结构

```text
.
├── Models/
│   └── AnalysisModels.cs
├── Options/
│   └── AppStorageOptions.cs
├── Pages/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   └── Shared/
├── Services/
│   ├── WordAnalysisService.cs
│   ├── WordComparisonService.cs
│   ├── WordAnnotationService.cs
│   ├── ReportRenderService.cs
│   ├── NormalDocumentService.cs
│   ├── WordConversionService.cs
│   ├── AnalysisSessionRepository.cs
│   └── ...
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── lib/
├── Program.cs
├── WordFormatAnalyzer.csproj
├── appsettings.json
└── start-web.bat
```

## 整体架构

系统采用单体 Web 应用结构，页面层、业务服务层、模型层和本地存储层在同一个 ASP.NET Core 项目中。各层职责清晰，核心 Word 处理逻辑集中在 `Services/`，页面只负责上传、表单绑定、流程调度和下载。

```text
Browser
  |
  v
Razor Pages (Pages/Index.cshtml + Index.cshtml.cs)
  |
  v
Application Services (Services/)
  |
  +--> Word analysis and style resolution
  +--> Template rule extraction/editing/conflict resolution
  +--> Target document comparison
  +--> Word annotation and normal document generation
  +--> HTML report rendering
  |
  v
Local Storage (App_Data, Uploads, Generated, SQLite)
```

### 页面层

`Pages/Index.cshtml` 是主要交互界面，提供模板上传、自定义规则上传、参考模板上传、规则编辑、冲突选择、实际文档上传、报告展示和文件下载入口。

`Pages/Index.cshtml.cs` 是页面模型，负责：

- 校验上传文件类型和大小。
- 保存上传文件。
- 调用 `.doc` 转换服务。
- 调用模板分析、规则服务、比对服务和报告服务。
- 将会话保存到 SQLite。
- 提供带批注 Word、HTML 报告和正常文档下载。

### 模型层

`Models/AnalysisModels.cs` 定义系统中的核心数据结构：

- `DocumentAnalysisResult`：单个文档的分析结果。
- `ParagraphSnapshot`：段落文本、样式、字体、间距、区域和模块分类。
- `RunFormatSnapshot`：文本运行级别的字体、字号和字符统计。
- `TableSnapshot`：表格结构摘要。
- `PageSetupSnapshot`：页边距、页眉页脚、纸张大小和方向。
- `TemplateFormatRule`：按区域/模块提取出的模板规则。
- `TemplateRuleConflict`：多个模板之间的规则冲突。
- `FormatIssue`：单条格式问题。
- `AnalysisReport`：检测报告。
- `AnalysisSession`：一次用户分析会话。

### 服务层

`Services/` 是业务核心：

- `WordAnalysisService`：使用 Open XML 读取 Word 文档，提取批注、段落、文本运行、表格、页面设置、大空白、覆盖区域和模板规则。
- `WordStyleResolver`：解析段落和文本运行的直接格式及样式继承结果。
- `WordTerminologyService`：将 Open XML 原始值转换为 Word 用户更容易理解的术语，例如字号、段落间距和页面方向。
- `TemplateRuleService`：从文档分析结果生成模块化格式规则，并应用用户编辑。
- `TemplateRuleValueNormalizer`：把用户输入的字号、字体、缩进、行距等规则归一化为可比对值。
- `TemplateRuleConflictService`：发现总模板和参考模板之间的规则差异，并处理用户选择。
- `EffectiveTemplateService`：构建本次检查实际使用的模板规则。
- `WordComparisonService`：根据模板规则和目标文档生成 `FormatIssue` 列表，并汇总严重级别、类型和区域统计。
- `PunctuationIssueService`：检查中英文标点、重复标点、英文标点空格和句末标点等问题。
- `IssueTextService`：生成报告标题、期望值、实际值和 Word 批注文本。
- `WordAnnotationService`：复制目标文档并在问题位置写入 Word 批注。
- `ReportRenderService`：生成离线 HTML 报告。
- `NormalDocumentService`：在安全范围内生成按规则调整后的正常 Word 文档。
- `WordConversionService`：处理 `.doc` 到 `.docx` 的转换。
- `AppStorage`：创建上传文件、生成文件和工作文件路径。
- `DatabaseInitializer`：初始化 SQLite 表结构。
- `AnalysisSessionRepository`：保存和读取分析会话。

### 存储层

存储路径由 `appsettings.json` 中的 `Storage` 节配置：

```json
{
  "Storage": {
    "DataDirectory": "App_Data",
    "UploadDirectory": "Uploads",
    "GeneratedDirectory": "Generated",
    "DatabaseFileName": "word-analyzer.db"
  }
}
```

运行时会在 `App_Data/` 下创建：

- `Uploads/`：用户上传的模板、参考模板和待检查文档。
- `Generated/`：带批注 Word、HTML 报告、正常 Word 文档和中间转换文件。
- `word-analyzer.db`：SQLite 会话数据库。

这些运行时文件不应提交到 Git。

## 处理流程

模板检查的核心流程：

```text
Upload template
  -> Save file
  -> Convert .doc to .docx when needed
  -> Analyze template with Open XML
  -> Extract baseline and module rules
  -> Persist session
  -> Optional reference template analysis
  -> Optional conflict resolution and rule editing
  -> Upload target document
  -> Analyze target document
  -> Build effective template
  -> Compare target against template rules
  -> Generate annotated Word
  -> Generate HTML report
  -> Generate normal document
  -> Persist report and generated file paths
```

自定义规则检查的核心流程：

```text
Upload target document
  -> Analyze and classify modules
  -> Build empty editable rules
  -> User fills module rules
  -> Compare document against user rules
  -> Generate annotated Word, HTML report, and normal document
```

## 检查范围

当前代码覆盖的主要检查项包括：

- 中文字体和西文字体。
- 字号。
- 段落对齐。
- 段前、段后、行距和首行缩进。
- 文本运行级别的局部字体和字号异常。
- 页面设置差异。
- 表格列数差异。
- 连续空段落导致的大空白。
- 中英文标点和句末标点问题。
- 无法完全自动判断的项目提醒。

## 输出文件

一次检查完成后，页面可下载：

- `*-格式检查批注.docx` 或 `*-自定义规则批注.docx`：带问题批注的 Word 文档。
- `*-格式检测报告.html` 或 `*-自定义规则检测报告.html`：离线 HTML 报告。
- `*-正常文档.docx` 或 `*-按自定义规则调整.docx`：按规则自动调整后的 Word 文档。

## 配置说明

默认配置在 `appsettings.json`：

- `Logging`：ASP.NET Core 日志等级。
- `AllowedHosts`：主机限制。
- `Storage`：运行时数据目录、上传目录、生成目录和 SQLite 数据库名称。

如需本地覆盖配置，可创建未提交的 `appsettings.Local.json` 或使用环境变量。

## 开发与验证

常用命令：

```bash
dotnet restore
dotnet build
dotnet run --urls "http://localhost:5088"
```

建议验证项：

- 首页可以打开。
- 模板上传后显示模板分析摘要。
- 参考模板上传后可显示冲突或合并规则。
- 规则编辑后可以保存。
- 实际文档上传后生成报告。
- 带批注 Word、HTML 报告、正常文档下载可用。

## 注意事项

- `.doc` 转换依赖 Windows 和 Microsoft Word；非 Windows 环境请使用 `.docx`。
- 当前应用定位为本地或内网工具，没有内置用户账户和权限系统。
- 上传文件和生成文件保存在本地运行目录下，不适合直接作为公开互联网服务部署。
- Open XML 无法完整模拟 Word 的分页排版结果，因此目录页码、复杂浮动对象、图片分页等场景可能需要人工复核。

