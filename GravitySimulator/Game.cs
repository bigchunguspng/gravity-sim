using Raylib_cs;

namespace GravitySimulator;

public class Game
{
    private const int
        W = 640,
        H = 480,
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
                    var m = s.Mass[i]; // vol sphere = 4/3 pi rrr   r = root3(3 mass / 4 pi)
                    var r = (float)Math.Pow(0.75 * m / Math.PI, 1 / 3.0);
                    Raylib.DrawCircle(x, y, r, Color.White);
                }
                Raylib.DrawFPS(10, 10);
            }
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}