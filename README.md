# BackgroudProcess.Manager（bpm）

一个极简的命令行后台进程管理器。通过命令行登记若干「名字 → 可执行文件路径」的条目，并用 `update` 命令按 `autoRun` 开关自动拉起 / 停止进程，适合放进计划任务或启动脚本里周期性执行。

- 程序名（AssemblyName）：`bpm`
- 版本：`v0.0.2`
- 作者：`cglang`
- 目标框架：.NET 10
- 发布方式：AOT（`PublishAot=true`），发布产物无运行时依赖

## 配置文件

配置为纯文本，一行一条记录，默认位置：

| 环境 | 配置文件位置 |
| --- | --- |
| Release 编译 | `~/.config/drag.conf` |
| DEBUG 编译 | 程序所在目录 `drag.conf` |

每行格式（由 `|` 分隔的键值对）：

```
pid=12345|state=1|autoRun=True|name=nginx|binPath=C:\tools\nginx.exe
```

| 字段 | 说明 |
| --- | --- |
| `pid` | 最近一次启动的进程 ID，`0` 表示未启动 |
| `state` | `0` = Stop，`1` = Run |
| `autoRun` | `true` / `false`，是否期望保持运行 |
| `name` | 条目名，命令中用它来引用 |
| `binPath` | 可执行文件的完整路径（工作目录取该文件所在目录） |

> 首次运行时如果配置文件不存在，程序会创建一个空文件并提示一行错误，再次执行命令即可。

## 安装 / 构建

```bash
# 调试构建
dotnet build

# 发布（AOT，生成单个可执行文件 bpm）
dotnet publish -c Release -r win-x64
```

发布产物默认在 `BackgroudProcess.Manager/bin/Release/net10.0/publish/`，把 `bpm(.exe)` 放到 PATH 中即可全局使用。

## 命令用法

```bash
bpm                         # 等同于 bpm list：以表格列出全部配置
bpm list | ls | status      # 同上
bpm update                  # 按 autoRun 同步所有进程
bpm add <name> <binPath> [autoRun]
bpm run | start <编号|名称>   # 启动
bpm stop <编号|名称>          # 停止
bpm remove <编号|名称>        # 删除
bpm edit <编号|名称> <binPath|autoRun> <value>
bpm version                 # 查看版本号（也可用 -v / --version）
```

`<编号>` 就是表格里从上往下的「序号」（从 1 开始），也可以直接写名称；数字优先按编号解析。

参数无法识别时会打印命令帮助。

### 无参数 / list — 查看配置列表

```bash
bpm
```

输出表格：

```
序号  名称    运行状态  路径                             自动运行
----  ------  --------  -------------------------------  --------
1     nginx   运行中    C:\tools\nginx\nginx.exe         是
2     记事本  已停止    C:\Windows\System32\notepad.exe  否
```

「运行状态」是根据已记录的 `pid` 实时判断进程是否存活得出的；路径过长时会截断显示（不改动配置）。

### update — 声明式同步（核心命令）

```bash
bpm update
```

遍历所有条目：

- `autoRun=true` 且进程未运行 → 启动它
- `autoRun=false` 且进程正在运行 → 杀掉它

执行过程中会打印 `启动:<name>` / `停止:<name>`。适合做成定时任务反复调用，实现「保持运行」的效果。

### add — 新增条目

```bash
bpm add api "C:\apps\api\api.exe" true
```

- `autoRun` 可选，缺省 `false`，只接受 `True`/`False`（不区分大小写）
- 名称已存在时会拒绝添加
- 新增的条目不检查进程，`pid` 记为 `0`、状态为 `Stop`

### remove — 删除条目

```bash
bpm remove 2        # 删掉编号为 2 的条目
bpm remove api      # 也可以按名称
```

只删除配置条目，不会去杀进程；如需先停止请先执行 `bpm stop <name>`。

### start / stop — 手动控制

```bash
bpm run 1         # 立即启动编号 1（run 与 start 等价）
bpm stop 1        # 杀掉进程，pid 归零，autoRun 不变
```

`start` 会写回 `state=Run`，但**不会**修改 `autoRun`，因此下一次 `bpm update` 若 `autoRun=false` 会把它再停掉。

### edit — 修改条目字段

```bash
bpm edit 1 "C:\apps\api\api-v2.exe"   # 改 binPath
bpm edit 1 autoRun true               # 改 autoRun
```

只支持修改 `binPath` 与 `autoRun` 两个字段，其余字段会提示错误。字段名不区分大小写。

## 常见用法示例

```bash
# 1. 注册两个常驻服务
bpm add web "C:\apps\web\web.exe" true
bpm add worker "C:\apps\worker\worker.exe" true

# 2. 立刻全部拉起（也可以直接 start 单个）
bpm update

# 3. 查看是否都在跑
bpm

# 4. 让 worker（假设编号 2）不再自动运行，并停掉它
bpm edit 2 autoRun false
bpm update

# 5. 彻底移除 web（假设编号 1）
bpm stop 1
bpm remove 1
```

配合 Windows 计划任务，每隔几分钟执行一次 `bpm update` 即可实现进程守护。

## 注意事项

- `stop` 与 `update` 停止进程时使用 `Process.Kill()`（强制结束，不发送优雅退出信号）。
- `pid` 会被持久化，因此机器重启后若 PID 被其他进程复用，可能出现状态误判；此时对目标执行一次 `stop` 或在 `update` 前用 `start` 重置即可。
- 所有 `binPath` 请使用绝对路径，启动时会把工作目录设置为该文件所在目录。
- 命令执行结束都会把当前内存中的条目整体写回配置文件（包括 `remove` / `edit` 等操作）。
