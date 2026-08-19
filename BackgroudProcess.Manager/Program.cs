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

        static async Task Main(string[] args)
        {
#if DEBUG
            if (args.Length == 0)
                args = new[] { "status" };
#endif

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
            var options = DragOptionsManager.GetDragOptions().ToList();

            var keys = new[] { "update", "status", "add", "remove", "start", "stop", "edit" };

            var key = string.Empty;
            if (args.Any())
            {
                if (keys.Contains(args[0])) key = args[0];
                else Console.WriteLine("""
                    - status    查看状态
                    - update    更新
                    - add <name> <binPath> [autoRun]
                    - remove <name>
                    - start <name>
                    - stop <name>
                    - edit <name> <binPath|autoRun> <value>
                    """);
            }
            else
            {
                key = "update";
            }

            switch (key)
            {
                case "status":
                    foreach (var drag in options)
                    {
                        Console.WriteLine($"{drag.Name}:{ProcessUtil.GetState(drag.Pid)}");
                    }
                    break;
                case "update":
                    foreach (var drag in options)
                    {
                        if (drag.AutoRun && ProcessUtil.GetState(drag.Pid) == State.Stop)
                        {
                            var pid = ProcessUtil.StartProcess(drag.BinPath);
                            drag.Pid = pid;
                            drag.State = State.Run;
                            Console.WriteLine($"启动:{drag.Name}");
                        }
                        if (!drag.AutoRun && ProcessUtil.GetState(drag.Pid) == State.Run)
                        {
                            ProcessUtil.StopProcess(drag.Pid);
                            drag.Pid = 0;
                            drag.State = State.Stop;
                            Console.WriteLine($"停止:{drag.Name}");
                        }
                    }
                    break;
                case "add":
                    // args: add <name> <binPath> [autoRun]
                    if (args.Length < 3)
                    {
                        Console.WriteLine("用法: add <name> <binPath> [autoRun]");
                        break;
                    }
                    {
                        var name = args[1];
                        var binPath = args[2];
                        var autoRun = false;
                        if (args.Length >= 4) bool.TryParse(args[3], out autoRun);

                        if (options.Any(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.WriteLine($"已存在同名项: {name}");
                            break;
                        }

                        var newOpt = new DragOptions
                        {
                            Pid = 0,
                            Name = name,
                            State = State.Stop,
                            AutoRun = autoRun,
                            BinPath = binPath
                        };
                        options.Add(newOpt);
                        Console.WriteLine($"已添加: {name}");
                    }
                    break;
                case "remove":
                    // args: remove <name>
                    if (args.Length < 2)
                    {
                        Console.WriteLine("用法: remove <name>");
                        break;
                    }
                    {
                        var name = args[1];
                        var removed = options.RemoveAll(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (removed > 0) Console.WriteLine($"已移除: {name}");
                        else Console.WriteLine($"未找到: {name}");
                    }
                    break;
                case "start":
                    // args: start <name>
                    if (args.Length < 2)
                    {
                        Console.WriteLine("用法: start <name>");
                        break;
                    }
                    {
                        var name = args[1];
                        var opt = options.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (opt == null)
                        {
                            Console.WriteLine($"未找到: {name}");
                            break;
                        }
                        if (ProcessUtil.GetState(opt.Pid) == State.Run)
                        {
                            Console.WriteLine($"已在运行: {name}");
                            break;
                        }
                        var pid = ProcessUtil.StartProcess(opt.BinPath);
                        opt.Pid = pid;
                        opt.State = State.Run;
                        Console.WriteLine($"已启动: {name}");
                    }
                    break;
                case "stop":
                    // args: stop <name>
                    if (args.Length < 2)
                    {
                        Console.WriteLine("用法: stop <name>");
                        break;
                    }
                    {
                        var name = args[1];
                        var opt = options.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (opt == null)
                        {
                            Console.WriteLine($"未找到: {name}");
                            break;
                        }
                        if (ProcessUtil.GetState(opt.Pid) == State.Stop)
                        {
                            Console.WriteLine($"未在运行: {name}");
                            break;
                        }
                        ProcessUtil.StopProcess(opt.Pid);
                        opt.Pid = 0;
                        opt.State = State.Stop;
                        Console.WriteLine($"已停止: {name}");
                    }
                    break;
                case "edit":
                    // args: edit <name> <binPath|autoRun> <value>
                    if (args.Length < 4)
                    {
                        Console.WriteLine("用法: edit <name> <binPath|autoRun> <value>");
                        break;
                    }
                    {
                        var name = args[1];
                        var field = args[2];
                        var value = args[3];
                        var opt = options.FirstOrDefault(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (opt == null)
                        {
                            Console.WriteLine($"未找到: {name}");
                            break;
                        }
                        switch (field.ToLowerInvariant())
                        {
                            case "binpath":
                                opt.BinPath = value;
                                Console.WriteLine($"已更新 binPath: {name}");
                                break;
                            case "autorun":
                                if (bool.TryParse(value, out var ar))
                                {
                                    opt.AutoRun = ar;
                                    Console.WriteLine($"已更新 autoRun: {name}");
                                }
                                else Console.WriteLine("autoRun 必须是 true 或 false");
                                break;
                            default:
                                Console.WriteLine("只支持编辑: binPath, autoRun");
                                break;
                        }
                    }
                    break;
            }

            await DragOptionsManager.SaveOptionsAsync(options);
        }
    }
}
