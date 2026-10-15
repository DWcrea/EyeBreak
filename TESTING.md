# EyeBreak 测试记录（2026-10-15）

环境：Windows x64；项目局部 .NET SDK 10.0.402；Windows Desktop Runtime 10.0.12。Release 构建：**0 警告、0 错误**。没有系统级安装 SDK。所有测试均在本机执行，不将设计推断当成实测。

## V1.1.0 新增功能

### 休息时显示桌面（MinimizeAll / UndoMinimizeAll）

`--verify-shell` 受控实测：测试进程自建一个可见窗口作为判据。

```text
PASS 测试窗口已创建且处于正常状态
桌面已最小化的可见顶层窗口：4 个
PASS MinimizeAll 生效（等同 Win+D），受控窗口已最小化；桌面 4 → 6 个
PASS UndoMinimizeAll 还原，受控窗口恢复为正常状态；桌面 4 个
```

`--verify-break` 端到端实测（直接运行发布产物，配置 MinimizeWindowsOnBreak=true，测试模式 20 秒工作 / 5 秒休息）：

```text
PASS 探针窗口已创建且处于正常状态（用于判断桌面是否被最小化）
PASS 测试模式下 20.8 秒后出现休息提醒窗口
PASS 休息开始时所有窗口被最小化，桌面已显示
PASS 提醒窗口在最小化桌面后仍然可见，未被连带最小化
PASS 休息窗口约 4.5 秒后自动关闭
PASS 休息结束后窗口自动还原到休息前的状态
```

其中第 4 项是关键风险点：若提醒窗被连带最小化，用户只会看到空桌面。已确认不会发生。
测试使用的配置文件在测试前备份、测试后原样恢复；测试用探针窗口由测试进程自行创建并销毁。

### 托盘悬浮显示剩余时间

由 `DisplayText` 单元测试覆盖，逐项校验文本内容与长度上限（NotifyIcon 提示的 Win32 长度限制）：

```text
PASS 托盘提示显示剩余时间
PASS 托盘提示长度不超过系统上限
```

## V1.0.0 既有功能

| 教程验收点 | 结果与验证方式 |
|---|---|
| 休息时最小化所有窗口（新） | 通过：受控测试 + 端到端实测，见上。 |
| 提醒窗不被连带最小化（新） | 通过：端到端实测确认仍可见。 |
| 休息结束自动还原窗口（新） | 通过：端到端实测确认探针窗口恢复。 |
| 托盘悬浮显示剩余时间（新） | 通过：单元测试校验文本与长度；未用鼠标实际悬停目视确认。 |
| Windows 发布版启动、无黑框 | 通过：实测发布版进程启动并持续运行；项目类型为 WinExe。未做屏幕录制。 |
| 托盘图标出现，菜单可用，双击设置 | 未人工目视/点击验证（当前会话仅做 Win32 窗口探测）；代码创建可见 NotifyIcon 及中文菜单。 |
| 测试模式 20 秒工作后弹窗 | 通过：UI 冒烟测试运行发布 EXE，约 19.0 秒检测到可见休息窗口（采样/启动时刻误差）。 |
| 休息约 5 秒关闭、重新计时 | 通过：发布版窗口约 5.0 秒关闭；状态机测试验证进入新周期。 |
| 正式 20 分钟 / 20 秒配置 | 通过：默认配置及状态机参数测试；实际等待 20 分钟未验证。 |
| 手动暂停、恢复保留进度 | 通过：状态机自动测试；托盘菜单人工点击未验证。 |
| 空闲暂停、活动恢复 | 通过：模拟空闲输入的状态机自动测试；实际静置 120 秒未验证。 |
| 锁屏 / 休眠唤醒不连续弹窗 | 部分验证：代码订阅 Windows 会话/电源事件并丢弃过长时钟间隔；未执行真实锁屏及休眠试验。 |
| 第 3 轮长休息替代短休息 | 通过：状态机测试连续模拟三轮，长休息只出现一次。 |
| 推迟 5 分钟且无重复弹窗 | 通过：状态机测试模拟 299 秒、到期时空闲及再次活动。实际弹窗按钮点击未验证。 |
| 保存设置重启仍生效 | 通过：配置读写测试使用临时测试文件重新加载；实际设置窗口人工操作未验证。 |
| 损坏配置恢复默认 | 通过：临时文件写入无效 JSON，加载自动恢复并回写默认值。 |
| 开机自启写入/删除 | 未验证：避免自动测试修改用户注册表；实现只操作 HKCU Run 的 EyeBreak 值。建议用户自行切换并检查。 |
| 第二次启动不产生双计时实例 | 通过：实际双启发布版，第二进程正常退出、首进程继续运行。 |
| 退出后进程及托盘图标消失 | 部分验证：退出逻辑释放托盘图标；测试进程由测试程序结束，未实际点击“退出”。 |
| 发布目录直接运行 | 通过：冒烟测试直接执行发布目录的 EXE。 |
| 说明运行环境 | 通过：README 明确 .NET 10 Desktop Runtime x64。 |

自动化测试：**13/13 通过**；UI 冒烟测试：启动、单实例、休息弹窗、自动关闭通过；该次实例工作集约 **65.3 MiB**（仅单次测量，随机器和运行状态变化）。没有为追求小体积开启 trimming/AOT，也没有任何后台联网功能。

构建发布命令：

```powershell
dotnet build .\EyeBreak.sln -c Release
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release
dotnet publish .\EyeBreak\EyeBreak.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugSymbols=false -o .\outputs\win-x64-small
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release -- --ui-smoke .\outputs\win-x64-small\EyeBreak.exe
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release -- --verify-shell
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release -- --verify-break .\outputs\win-x64-small\EyeBreak.exe
```
