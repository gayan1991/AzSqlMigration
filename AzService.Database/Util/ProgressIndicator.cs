namespace AzService.Database.Util
{
    public class ProgressIndicator
    {
        private static ProgressIndicator _instance;
        public static void Show()
        {
            _instance = new ProgressIndicator();
            return;
        }

        public static void Hide()
        {
            _instance.Dispose();
            _instance = null;
            GC.Collect();
        }

        private readonly Timer _timer;

        ProgressIndicator()
        {
            _timer = new Timer(TimerCallback, null, 0, 1000);
        }

        private void Dispose()
        {
            _timer.Dispose();
            ClearProgressIndicator();
        }

        private void TimerCallback(Object o)
        {
            Console.Write(".");
        }

        private void ClearProgressIndicator()
        {
            var currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
