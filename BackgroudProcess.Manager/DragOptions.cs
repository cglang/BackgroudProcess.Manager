namespace BackgroudProcess.Manager
{
    public class DragOptions
    {
        public int Pid { get; set; }

        public string Name { get; set; } = string.Empty;

        public State State { get; set; }

        public bool AutoRun { get; set; }

        public string BinPath { get; set; } = string.Empty;

        public static DragOptions Parse(string config)
        {
            var pairs = config
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(split => split.Length == 2)
                .ToDictionary(split => split[0], split => split[1]);

            return new()
            {
                Pid = pairs.TryGetValue("pid", out var pid) && int.TryParse(pid, out var p) ? p : 0,
                Name = pairs.TryGetValue("name", out var name) ? name : string.Empty,
                State = pairs.TryGetValue("state", out var state) && Enum.TryParse<State>(state, out var s) ? s : State.Stop,
                AutoRun = pairs.TryGetValue("autoRun", out var autoRun) && bool.TryParse(autoRun, out var a) && a,
                BinPath = pairs.TryGetValue("binPath", out var binPath) ? binPath : string.Empty,
            };
        }

        public override string ToString()
        {
            return $"pid={Pid}|state={(int)State}|autoRun={AutoRun}|name={Name}|binPath={BinPath}";
        }
    }

    public enum State
    {
        Stop = 0,
        Run = 1
    }
}
