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
            var keyValuePairs = config
                .Split('|')
                .Select(part => part.Split('='))
                .ToDictionary(split => split[0], split => split[1]);

            return new()
            {
                Pid = int.Parse(keyValuePairs["pid"]),
                Name = keyValuePairs["name"],
                State = Enum.Parse<State>(keyValuePairs["state"]),
                AutoRun = bool.Parse(keyValuePairs["autoRun"]),
                BinPath = keyValuePairs["binPath"],
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
