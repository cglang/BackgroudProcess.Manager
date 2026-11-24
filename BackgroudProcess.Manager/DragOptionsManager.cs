namespace BackgroudProcess.Manager
{
    public class DragOptionsManager
    {
        private static readonly string configPath = Path.Combine(Program.BasePath, "drag.conf");

        public static List<DragOptions> GetDragOptions()
        {
            if (!File.Exists(configPath))
            {
                Directory.CreateDirectory(Program.BasePath);
                using (File.Create(configPath)) { }
                return new List<DragOptions>();
            }

            return File.ReadAllLines(configPath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(DragOptions.Parse)
                .ToList();
        }

        public static async Task SaveOptionsAsync(List<DragOptions> dragOptions)
        {
            var configs = dragOptions.Select(x => x.ToString());
            await File.WriteAllLinesAsync(configPath, configs);
        }
    }
}
