namespace JSONScript.Compiler.Diagnostics
{
    public class DiagnosticBag
    {
        private readonly List<(string file, string message)> errors = new();

        public bool HasErrors => errors.Count > 0;

        public void Report(string file, string message) => errors.Add((file, message));

        public void PrintAll()
        {
            foreach (var (file, message) in errors)
            {
                Console.Error.WriteLine($"{file}: {message}");
            }
        }
    }
}