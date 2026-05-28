# Repository File Guide / 仓库文件整理说明

## Should be committed / 应提交

- `Models/`, `Services/`, `Pages/`, `Options/`, `Program.cs`
- `WordFormatAnalyzer.csproj`
- `wwwroot/css/`, `wwwroot/js/`, `wwwroot/lib/`
- `appsettings.json`, `appsettings.Development.json`
- `README.md`, `.gitignore`, `REPOSITORY_FILES.md`
- `.planning/` 产品需求和规划文档（如保留）

## Should not be committed / 不应提交

- `bin/`, `obj/`, `out/`, `publish/`
- `App_Data/`, `Uploads/`, `Generated/`
- SQLite 数据库和运行时文件：`*.db`, `*.sqlite`, `*.sqlite3`, `*.db-wal`, `*.db-shm`
- 上传的 Word 文档、检测报告、正常文档、HTML smoke 测试输出
- `.vs/`, `.vscode/`, `*.user`, `*.suo`
- `.env*`, `appsettings.Local.json`, `secrets.json`
- Local workflow-tool metadata files or directories

## Notes / 说明

`wwwroot/lib/` 当前没有 `libman.json` 或 `package.json` 可自动恢复来源，因此作为项目静态依赖上传。后续如果改用 LibMan、npm 或 CDN 管理前端依赖，可以再调整为忽略该目录。
