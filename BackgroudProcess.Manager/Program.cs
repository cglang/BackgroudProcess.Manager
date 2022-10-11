using Ddon.Core.Use;

namespace BackgroudProcess.Manager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var fullname = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            var keyValues = new DdonDictionary<Drag>(fullname);

            if (!keyValues.Any())
            {
                var drag = new Drag(false, "Test.exe", "Test");
                keyValues.Add(Guid.NewGuid().ToString(), drag);
                keyValues.SaveAsync().Wait();
            }

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
                    foreach (var drag in keyValues)
                    {
                        Console.WriteLine($"{drag.Value.Name}:{ProcessUtil.GetState(drag.Value.Pid)}");
                    }
                    break;
                case "update":
                    foreach (var drag in keyValues)
                    {
                        if (drag.Value.AutoRun && ProcessUtil.GetState(drag.Value.Pid) == State.Stop)
                        {
                            var pid = ProcessUtil.StartProcess(drag.Value.Command);
                            drag.Value.Pid = pid;
                            drag.Value.State = State.Run;
                            keyValues.SaveAsync().Wait();
                            Console.WriteLine($"启动:{drag.Value.Name}");
                        }
                        if (!drag.Value.AutoRun && ProcessUtil.GetState(drag.Value.Pid) == State.Run)
                        {
                            ProcessUtil.StopProcess(drag.Value.Pid);
                            drag.Value.Pid = 0;
                            drag.Value.State = State.Stop;
                            keyValues.SaveAsync().Wait();
                            Console.WriteLine($"停止:{drag.Value.Name}");
                        }
                    }
                    break;
            }

            keyValues.SaveAsync().Wait();
        }
    }
}