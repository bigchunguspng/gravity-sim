using Raylib_cs;

namespace GravitySimulator;

public class Game
{
    private const int
        W = 1280,
        H = 960,
        M = 25;

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(W, H, "Gravity Simulator");
        Raylib.SetTargetFPS(60);

        const int count = 25;
        var s = new Simulation();
        s.Init(count, W, H);

        while (!Raylib.WindowShouldClose())
        {
            // INPUT
            var restart = Raylib.IsKeyPressed(KeyboardKey.Enter);
            if (restart)
            {
                s.Init(count, W, H);
            }

            // LOGIC
            s.Tick();

            // RENDER
            Raylib.BeginDrawing();
            {
                Raylib.ClearBackground(Color.Black);
                for (var i = 0; i < count; i++)
                {
                    var x = (int)s.PX[i];
                    var y = (int)s.PY[i];
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