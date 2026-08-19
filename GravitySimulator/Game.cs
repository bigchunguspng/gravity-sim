using System.Numerics;
using Raylib_cs;

namespace GravitySimulator;

public class Game
{
    private const int
        W = 1280,
        H = 960,
        M = 25;

    private const bool
        SUN = true,
        FOLLOW_SUN_DEFAULT = true;

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(W, H, "Gravity Simulator");
        Raylib.SetTargetFPS(60);

        const int count = 100;
        var s = new Simulation();
        s.Init(count, W, H, SUN);

        var mouse_down_prev_frame = false;
        var offset = new Vector2();
        var follow_sun = FOLLOW_SUN_DEFAULT;

        while (!Raylib.WindowShouldClose())
        {
            // INPUT
            var restart = Raylib.IsKeyPressed(KeyboardKey.Enter);
            if (restart)
            {
                s.Init(count, W, H, SUN);
                offset = new Vector2();
            }

            var toggle_follow_sun = Raylib.IsKeyPressed(KeyboardKey.Backslash);
            if (toggle_follow_sun)
            {
                follow_sun = !follow_sun;
            }

            if (follow_sun)
            {
                var c = Raylib.GetScreenCenter();
                offset.X = c.X - s.PX[0];
                offset.Y = c.Y - s.PY[0];
            }

            var mouse_down = Raylib.IsMouseButtonDown(MouseButton.Left);
            if (mouse_down && !mouse_down_prev_frame)
            {
                mouse_down_prev_frame = true;
            }
            else if (mouse_down && mouse_down_prev_frame)
            {
                offset += Raylib.GetMouseDelta();
            }
            else
            {
                mouse_down_prev_frame = false;
            }

            // LOGIC
            s.Tick();

            // RENDER
            Raylib.BeginDrawing();
            {
                Raylib.ClearBackground(Color.Black);
                for (var i = 0; i < count; i++)
                {
                    var x = (int)(offset.X + s.PX[i]);
                    var y = (int)(offset.Y + s.PY[i]);
                    var r = (float)s.GetParticleRadius(i, 2);
                    Raylib.DrawCircle(x, y, r, Color.White);
                }
                Raylib.DrawFPS(10, 10);
            }
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}