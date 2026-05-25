# Adaptive Music 协作约定

## 开发节奏

- 默认先做本地修改、本地构建、本地运行验证。
- 不要在每次小改后自动提交或推送。
- 只有当用户明确说“上传”“提交”“推送”“提交推送”等指令时，才执行 `git commit` 和 `git push`。
- 上传前必须先运行 `dotnet build`，确认没有错误；如果有警告，需要在回复中说明。
- 上传后回复提交哈希、仓库链接和工作区状态。

## 当前优先级

- 优先优化用户体验、控制台 UI、功能使用逻辑和状态反馈。
- 新功能要尽量集中到控制台页面，托盘只保留入口和退出。
- 任何音量写入都必须只影响“会被降低音量的应用”列表中的音乐程序。
- 其他音频会话只允许作为触发源，不允许被修改音量。

## 验证要求

- 修改核心音频逻辑后，需要测试降低和恢复是否正常。
- 修改 UI 后，需要启动应用确认窗口可打开、主要控件可见且文字不遮挡。
- 修改配置项后，需要确认现有 `%APPDATA%\AdaptiveMusic\config.json` 能兼容加载。

## Agent skills

### Issue tracker

Issues and PRDs are tracked in GitHub Issues for `Dave-oioioi/Adaptive_Music`. See `Docs/agents/issue-tracker.md`.

### Triage labels

Use the default five-label vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, and `wontfix`. See `Docs/agents/triage-labels.md`.

### Domain docs

This is a single-context repo; read root `CONTEXT.md` and `Docs/adr/` when they exist. See `Docs/agents/domain.md`.
