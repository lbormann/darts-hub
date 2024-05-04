using Avalonia;
using System;
using System.Threading;


namespace darts_hub
{


    internal class Program
    {
        private static Mutex _mutex;


        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) {
            const string MutexName = "YourAppName-UniqueMutexName";
            bool createdNew;

            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // Die Anwendung ist bereits gestartet.
                // Hier k�nnen Sie die gew�nschte Aktion ausf�hren, z.B. das Hauptfenster der anderen Instanz fokussieren.
                // Da Avalonia jedoch keine native Win32-API verwendet, ist es schwierig, das Hauptfenster der anderen Instanz direkt zu finden.
                // Eine m�gliche L�sung k�nnte die Verwendung von IPC (Inter-Process Communication) sein, um die beiden Instanzen miteinander kommunizieren zu lassen.
                return;
            }

            try
            {
                //BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                _mutex?.Close();
            }
        } 

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
