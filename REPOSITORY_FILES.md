# 仓库文件整理说明

## 应上传

- `Models/`, `Services/`, `Pages/`, `Options/`, `Program.cs`
- `WordFormatAnalyzer.csproj`
- `wwwroot/css/`, `wwwroot/js/`, `wwwroot/lib/`
- `appsettings.json`, `appsettings.Development.json`
- `.planning/` 开发规划文档
- `.beads/issues.jsonl`、`.beads/config.yaml`、`.beads/metadata.json`、`.beads/hooks/` 等 beads 协作数据
- `AGENTS.md`, `CLAUDE.md`, `.gitignore`, `REPOSITORY_FILES.md`

## 不应上传

- `bin/`, `obj/`, `out/`, `publish/`
- `App_Data/`, `Uploads/`, `Generated/`
- SQLite 数据库和运行时文件：`*.db`, `*.sqlite`, `*.sqlite3`, `*.db-wal`, `*.db-shm`
- 上传的 Word 文档、检测报告、正常文档、HTML smoke 测试输出
- `.vs/`, `.vscode/`, `*.user`, `*.suo`
- `.env*`, `appsettings.Local.json`, `secrets.json`
- `.beads/embeddeddolt/`, `.beads/backup/`, `.beads/interactions.jsonl` 等 beads 本机运行数据

## 说明

`wwwroot/lib/` 当前没有 `libman.json` 或 `package.json` 可自动恢复来源，因此作为项目静态依赖上传。后续如果改用 LibMan、npm 或 CDN 管理前端依赖，可以再调整为忽略该目录。
