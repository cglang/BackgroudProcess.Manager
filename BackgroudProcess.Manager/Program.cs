using System.Text;

namespace BackgroudProcess.Manager
{
    internal class Program
    {
#if DEBUG
        public static string BasePath => AppDomain.CurrentDomain.BaseDirectory;
#else
        public static string BasePath
        {
            get
            {
                var userpath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userpath, ".config");
            }
        }
#endif

        private const int MaxPathWidth = 80;

        static async Task Main(string[] args)
        {
            try
            {
                await Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[error]: {ex.Message}");
            }
        }

        static async Task Run(string[] args)
        {
            var options = DragOptionsManager.GetDragOptions();

            // 不带任何参数：列出全部配置
            if (args.Length == 0)
            {
                ShowList(options);
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "list":
                case "ls":
                case "status":
                    ShowList(options);
                    return;

                case "update":
                    Update(options);
                    break;

                case "add":
                    Add(options, args);
                    break;

                case "remove":
                case "rm":
                case "delete":
                    Remove(options, args);
                    break;

                case "start":
                case "run":
                    Start(options, args);
                    break;

                case "stop":
                    Stop(options, args);
                    break;

                case "edit":
                    Edit(options, args);
                    break;

                case "help":
                case "-h":
                case "--help":
                case "-?":
                    ShowHelp();
                    return;

                case "version":
                case "-v":
                case "--version":
                    Console.WriteLine(AppInfo.Display);
                    return;

                default:
                    Console.WriteLine($"未知命令: {args[0]}");
                    ShowHelp();
                    return;
            }

            await DragOptionsManager.SaveOptionsAsync(options);
        }

        /// <summary>按「自动运行」开关声明式同步所有进程。</summary>
        static void Update(List<DragOptions> options)
        {
            foreach (var drag in options)
            {
                if (drag.AutoRun && ProcessUtil.GetState(drag.Pid) == State.Stop)
                {
                    if (TryStart(drag, out var error)) Console.WriteLine($"启动:{drag.Name}");
                    else Console.WriteLine($"启动失败:{drag.Name} - {error}");
                }
                if (!drag.AutoRun && ProcessUtil.GetState(drag.Pid) == State.Run)
                {
                    ProcessUtil.StopProcess(drag.Pid);
                    drag.Pid = 0;
                    drag.State = State.Stop;
                    Console.WriteLine($"停止:{drag.Name}");
                }
            }
        }

        static void Add(List<DragOptions> options, string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("用法: add <名称> <路径> [true|false]");
                return;
            }

            var name = args[1];
            var binPath = args[2];
            var autoRun = args.Length >= 4 && bool.TryParse(args[3], out var parsed) && parsed;

            if (options.Any(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"已存在同名项: {name}");
                return;
            }

            options.Add(new DragOptions
            {
                Pid = 0,
                Name = name,
                State = State.Stop,
                AutoRun = autoRun,
                BinPath = binPath
            });

            Console.WriteLine($"已添加: {name}");
            ShowList(options);
        }

        static void Remove(List<DragOptions> options, string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("用法: remove <编号|名称>");
                return;
            }

            var target = Resolve(options, args[1]);
            if (target is null)
            {
                Console.WriteLine($"未找到: {args[1]}");
                return;
            }

            options.Remove(target);
            Console.WriteLine($"已移除: {target.Name}");
            ShowList(options);
        }

        static void Start(List<DragOptions> options, string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("用法: start|run <编号|名称>");
                return;
            }

            var opt = Resolve(options, args[1]);
            if (opt is null)
            {
                Console.WriteLine($"未找到: {args[1]}");
                return;
            }

            if (ProcessUtil.GetState(opt.Pid) == State.Run)
            {
                Console.WriteLine($"已在运行: {opt.Name} (pid={opt.Pid})");
                return;
            }

            if (!TryStart(opt, out var error))
            {
                Console.WriteLine($"启动失败: {opt.Name} - {error}");
                return;
            }

            Console.WriteLine($"已启动: {opt.Name} (pid={opt.Pid})");
            ShowList(options);
        }

        static void Stop(List<DragOptions> options, string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("用法: stop <编号|名称>");
                return;
            }

            var opt = Resolve(options, args[1]);
            if (opt is null)
            {
                Console.WriteLine($"未找到: {args[1]}");
                return;
            }

            if (ProcessUtil.GetState(opt.Pid) == State.Stop)
            {
                opt.Pid = 0;
                opt.State = State.Stop;
                Console.WriteLine($"未在运行: {opt.Name}");
                return;
            }

            ProcessUtil.StopProcess(opt.Pid);
            opt.Pid = 0;
            opt.State = State.Stop;
            Console.WriteLine($"已停止: {opt.Name}");
            ShowList(options);
        }

        static void Edit(List<DragOptions> options, string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("用法: edit <编号|名称> <binPath|autoRun> <值>");
                return;
            }

            var opt = Resolve(options, args[1]);
            if (opt is null)
            {
                Console.WriteLine($"未找到: {args[1]}");
                return;
            }

            var value = args[3];
            switch (args[2].ToLowerInvariant())
            {
                case "binpath":
                    opt.BinPath = value;
                    Console.WriteLine($"已更新 binPath: {opt.Name} -> {opt.BinPath}");
                    break;
                case "autorun":
                    if (!bool.TryParse(value, out var autoRun))
                    {
                        Console.WriteLine("autoRun 必须是 true 或 false");
                        return;
                    }
                    opt.AutoRun = autoRun;
                    Console.WriteLine($"已更新 autoRun: {opt.Name} -> {autoRun}");
                    break;
                default:
                    Console.WriteLine("只支持编辑: binPath, autoRun");
                    return;
            }

            ShowList(options);
        }

        static bool TryStart(DragOptions opt, out string error)
        {
            try
            {
                opt.Pid = ProcessUtil.StartProcess(opt.BinPath);
                opt.State = State.Run;
                error = string.Empty;
                return opt.Pid > 0;
            }
            catch (Exception ex)
            {
                opt.Pid = 0;
                opt.State = State.Stop;
                error = ex.Message;
                return false;
            }
        }

        /// <summary>参数先按编号（从 1 开始的序号）解析，再按名称匹配。</summary>
        static DragOptions? Resolve(List<DragOptions> options, string arg)
        {
            if (int.TryParse(arg, out var index))
                return index >= 1 && index <= options.Count ? options[index - 1] : null;

            return options.FirstOrDefault(o => o.Name.Equals(arg, StringComparison.OrdinalIgnoreCase));
        }

        static void ShowList(List<DragOptions> options)
        {
            if (options.Count == 0)
            {
                Console.WriteLine("暂无配置，使用: bpm add <名称> <路径> [true|false]");
                return;
            }

            var headers = new[] { "序号", "名称", "运行状态", "路径", "自动运行" };
            var rows = new List<string[]>(options.Count);

            for (var i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                rows.Add(new[]
                {
                    (i + 1).ToString(),
                    string.IsNullOrWhiteSpace(opt.Name) ? "-" : opt.Name,
                    ProcessUtil.GetState(opt.Pid) == State.Run ? "运行中" : "已停止",
                    string.IsNullOrWhiteSpace(opt.BinPath) ? "-" : Truncate(opt.BinPath, MaxPathWidth),
                    opt.AutoRun ? "是" : "否"
                });
            }

            var widths = new int[headers.Length];
            for (var c = 0; c < headers.Length; c++)
            {
                widths[c] = DisplayWidth(headers[c]);
                foreach (var row in rows)
                    widths[c] = Math.Max(widths[c], DisplayWidth(row[c]));
            }

            Console.WriteLine(Row(headers, widths));
            Console.WriteLine(string.Join("  ", widths.Select(w => new string('-', w))));
            foreach (var row in rows)
                Console.WriteLine(Row(row, widths));
        }

        static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("""
                用法: bpm [命令] [参数]

                  bpm / bpm list               列出全部配置（表格）
                  bpm update                   按「自动运行」开关启动/停止（计划任务用）
                  bpm add <名称> <路径> [true|false]
                  bpm run|start <编号|名称>    启动进程
                  bpm stop <编号|名称>         停止进程
                  bpm remove <编号|名称>       删除配置
                  bpm edit <编号|名称> <binPath|autoRun> <值>
                  bpm version                  查看版本号

                编号就是列表从上往下的序号（从 1 开始），也可以直接写名称。
                """);
        }

        static string Row(string[] cells, int[] widths)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < cells.Length; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(cells[i]);
                sb.Append(' ', Math.Max(0, widths[i] - DisplayWidth(cells[i])));
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>按控制台显示宽度计算（中日韩全角字符按 2 列）。</summary>
        static int DisplayWidth(string text)
        {
            var width = 0;
            foreach (var ch in text)
                width += IsWide(ch) ? 2 : 1;
            return width;
        }

        static bool IsWide(char ch) =>
            ch is >= '\u1100' and <= '\u115F'
                or >= '\u2E80' and <= '\uA4CF'
                or >= '\uAC00' and <= '\uD7A3'
                or >= '\uF900' and <= '\uFAFF'
                or >= '\uFE30' and <= '\uFE6F'
                or >= '\uFF00' and <= '\uFF60'
                or >= '\uFFE0' and <= '\uFFE6';

        static string Truncate(string text, int maxWidth)
        {
            if (DisplayWidth(text) <= maxWidth) return text;

            var sb = new StringBuilder();
            var width = 0;
            foreach (var ch in text)
            {
                var w = IsWide(ch) ? 2 : 1;
                if (width + w > maxWidth - 3) break;
                sb.Append(ch);
                width += w;
            }
            return sb.Append("...").ToString();
        }
    }
}
