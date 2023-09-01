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
                args = new string[] { "status" };
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

            var keys = new[] { "update", "status" };

            var key = string.Empty;
            if (args.Any())
            {
                if (keys.Contains(args[0])) key = args[0];
                else Console.WriteLine("status:状态 update:更新");
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
            }

            await DragOptionsManager.SaveOptionsAsync(options);
        }
    }
}