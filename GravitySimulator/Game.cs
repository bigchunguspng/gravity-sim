using System.Numerics;
using Raylib_cs;

namespace GravitySimulator;

public class Game
{
    private const int
        COUNT = 500,
        W = 1280,
        H = 960,
        FPS = 60,
        TRAIL_LEN_SEC = 10,
        TRAIL_LEN_FRAMES = TRAIL_LEN_SEC * FPS;

    private const bool
        SUN = true,
        FOLLOW_SUN_DEFAULT = true;

    private const float
        RADIUS_MULTIPLIER = 1.0F;

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(W, H, "Gravity Simulator");
        Raylib.SetTargetFPS(FPS);

        var s = new Simulation();
        s.Init(COUNT, W, H, SUN);

        var mouse_down_prev_frame = false;
        var offset = new Vector2();
        var follow_sun = SUN && FOLLOW_SUN_DEFAULT;
        var positions = new Vector2[COUNT];
        var trails = new Vector2[COUNT * TRAIL_LEN_FRAMES]; // 100p * 10s * 60f/s = 60000 pf * 8B = 480kB
        var trail_frame_last  = 0;
        var trail_frame_first = 0;

        while (!Raylib.WindowShouldClose())
        {
            // INPUT
            var restart = Raylib.IsKeyPressed(KeyboardKey.Enter);
            if (restart)
            {
                s.Init(COUNT, W, H, SUN);
                offset = new Vector2();
                Array.Clear(trails);
                trail_frame_last  = 0;
                trail_frame_first = 0;
            }

            var toggle_follow_sun = Raylib.IsKeyPressed(KeyboardKey.Backslash);
            if (toggle_follow_sun)
            {
                follow_sun = !follow_sun;

                if (follow_sun)
                {
                    Array.Clear(trails);
                    trail_frame_last  = 0;
                    trail_frame_first = 0;
                }
            }

            if (follow_sun)
            {
                var c = Raylib.GetScreenCenter();
                var sun_i = 0;
                if (!SUN) // get particle with max mass
                {
                    var max_mass = 0.0F;
                    for (var i = 0; i < COUNT; i++)
                    {
                        if (s.ON[i] && s.M[i] > max_mass)
                        {
                            max_mass = s.M[i];
                            sun_i = i;
                        }
                    }
                }
                offset.X = c.X - s.PX[sun_i];
                offset.Y = c.Y - s.PY[sun_i];

                mouse_down_prev_frame = false;
            }
            else
            {
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
            }

            // LOGIC
            {
                s.Tick();

                for (var i = 0; i < COUNT; i++)
                {
                    if (!s.ON[i]) continue;

                    var x = offset.X + s.PX[i];
                    var y = offset.Y + s.PY[i];
                    positions[i] = new Vector2(x, y);
                    trails[COUNT * trail_frame_last + i] = new Vector2(x, y);
                }
            }

            // RENDER
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                // draw trails
                var f_abs = trail_frame_first < trail_frame_last // 0 .. trail frames length
                    ?        TRAIL_LEN_FRAMES - trail_frame_last
                    : 0.0;
                for (var f = trail_frame_first; f != trail_frame_last; f = (f + 1) % TRAIL_LEN_FRAMES) // for each frame
                {
                    var f_percent = f_abs / TRAIL_LEN_FRAMES;
                    var f2 = (f + 1) % TRAIL_LEN_FRAMES;
                    for (var i = 0; i < COUNT; i++) // for each particle
                    {
                        if (!s.ON[i]) continue; // todo keep rest of trail (currently it will disappear) 

                        var v1 = trails[COUNT * f  + i]; // [f0 coords] [f1 coords] [...] [fN coords]
                        var v2 = trails[COUNT * f2 + i]; //             \---------\ L = count 
                        var v = (byte)(128 * f_percent);
                        Raylib.DrawLineEx(v1, v2, 1, new Color(v, v, v));
                    }

                    f_abs++;
                }

                // draw particles
                for (var i = 0; i < COUNT; i++)
                {
                    if (!s.ON[i]) continue;

                    var pos = positions[i];
                    var r = s.R[i] * RADIUS_MULTIPLIER;
                    var m = s.M[i];
                    var v = m < 1024
                        ? 1.0
                        : 1.0 - Math.Log(m / 1024.0) / Math.Log(32.0); // v(1k) = 1  v(32k) = 0
                    var blue = (int)(255 * Math.Clamp(v, 0, 1));
                    var color = new Color(255, 255, blue);
                    Raylib.DrawCircle((int)pos.X, (int)pos.Y, r, color);
                }

                Raylib.DrawText($"{s.ActiveCount}/{COUNT}", 10, 40, 20, Color.Blue);
                Raylib.DrawFPS(10, 10);
                Raylib.EndDrawing();
            }
            
            // LOGIC II
            {
                trail_frame_last = (trail_frame_last + 1) % TRAIL_LEN_FRAMES;

                if (trail_frame_first == trail_frame_last)
                    trail_frame_first = (trail_frame_first + 1) % TRAIL_LEN_FRAMES;
            }
        }
        Raylib.CloseWindow();
    }
}

// todo toggle follow sun to ON ?  offset trails to match sun pos (currently: clear)
// todo drag space ?  offset trails
// todo backspace = pause, launch particles with mouse