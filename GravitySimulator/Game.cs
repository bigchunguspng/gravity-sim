using Raylib_cs;

namespace GravitySimulator;

public class Game
{
    public void Run()
    {
        Raylib.InitWindow(640, 480, "Title");
        Raylib.SetTargetFPS(60);
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            {
                Raylib.ClearBackground(Color.Black);
            }
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();
    }
}