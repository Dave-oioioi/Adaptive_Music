using AdaptiveMusic.UI;

namespace AdaptiveMusic;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }    
}
