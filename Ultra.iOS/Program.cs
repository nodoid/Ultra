using Foundation;
using UIKit;
using Ultra.Core;

namespace Ultra.iOS;

/// <summary>
/// iOS entry point. Orientation is restricted to landscape in Info.plist and by the game.
/// </summary>
[Register("AppDelegate")]
internal class Program : UIApplicationDelegate
{
    private static UltraGame _game;

    internal static void RunGame()
    {
        _game = new UltraGame(new IosTiltSensor());
        _game.Run();
    }

    public override void FinishedLaunching(UIApplication app)
    {
        RunGame();
    }

    public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow forWindow) =>
        UIInterfaceOrientationMask.Landscape;

    private static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }
}
