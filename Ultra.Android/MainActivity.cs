using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using Microsoft.Xna.Framework;

using Ultra.Core;

namespace Ultra.Android;

/// <summary>
/// Android entry point. The activity is locked to landscape and runs full screen.
/// </summary>
[Activity(
    Label = "The Ultra",
    MainLauncher = true,
    Icon = "@mipmap/icon",
    RoundIcon = "@mipmap/icon",
    Theme = "@style/Theme.Splash",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden |
                           ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.UiMode
)]
public class MainActivity : AndroidGameActivity
{
    private UltraGame _game;
    private View _view;

    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        _game = new UltraGame();
        _view = _game.Services.GetService(typeof(View)) as View;

        SetContentView(_view);
        HideSystemUi();
        _game.Run();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
            HideSystemUi();
    }

    private void HideSystemUi()
    {
        if (Window == null)
            return;

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var controller = Window.InsetsController;
            if (controller != null)
            {
                controller.Hide(WindowInsets.Type.SystemBars());
                controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
        }
        else
        {
#pragma warning disable CA1422
            Window.DecorView.SystemUiFlags =
                SystemUiFlags.ImmersiveSticky | SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation |
                SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutStable;
#pragma warning restore CA1422
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window.Attributes != null)
        {
            var attrs = Window.Attributes;
            attrs.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            Window.Attributes = attrs;
        }
    }
}
