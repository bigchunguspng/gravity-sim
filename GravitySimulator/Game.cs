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
        SUN = false,
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

        var pause = false;
        var mouse_down_prev_frame = false;
        var offset = new Vector2();
        var follow_sun = SUN && FOLLOW_SUN_DEFAULT;
        var positions = new Vector2[COUNT];
        var trails    = new Vector2[COUNT * TRAIL_LEN_FRAMES]; // 100p * 10s * 60f/s = 60000 pf * 8B = 480kB
        var trails_lo = new  int[COUNT]; // leftover frames counts for dead particles: 600 -> 0
        var trails_td = new  int[COUNT]; // tfc value at particle death
        int trails_frame_newest; // n
        int trails_frame_oldest; // o
        int trails_frames_count; // tfc

        ResetTrails();

        void ResetTrails()
        {
            Array.Clear(trails);
            Array.Fill(trails_lo, -1);
            Array.Fill(trails_td, -1);
            trails_frame_newest = -1;
            trails_frame_oldest = 0;
            trails_frames_count = 0;
        }

        while (!Raylib.WindowShouldClose())
        {
            // INPUT
            var toggle_pause = Raylib.IsKeyPressed(KeyboardKey.Backspace);
            if (toggle_pause) pause = !pause;

            var step = pause && Raylib.IsKeyPressed(KeyboardKey.Equal);

            var restart = Raylib.IsKeyPressed(KeyboardKey.Enter);
            if (restart)
            {
                s.Init(COUNT, W, H, SUN);
                offset = new Vector2();
                ResetTrails();
            }

            var toggle_follow_sun = Raylib.IsKeyPressed(KeyboardKey.Backslash);
            if (toggle_follow_sun)
            {
                follow_sun = !follow_sun;

                if (follow_sun)
                {
                    ResetTrails();
                }
            }

            if (follow_sun)
            {
                var c = Raylib.GetScreenCenter();
                var sun_i = s.SunIndex;
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
            if (!pause || step)
            {
                trails_frame_newest = (trails_frame_newest + 1) % TRAIL_LEN_FRAMES;

                if (trails_frames_count == TRAIL_LEN_FRAMES)
                    trails_frame_oldest = (trails_frame_oldest + 1) % TRAIL_LEN_FRAMES;
                else
                    trails_frames_count++;

                s.Tick();

                for (var i = 0; i < COUNT; i++)
                {
                    if (!s.ON[i])
                    {
                        ref var
                            leftover_frames = ref trails_lo[i];
                        if (leftover_frames == -1)
                            leftover_frames = trails_td[i] = trails_frames_count;
                        if (leftover_frames > 0)
                            leftover_frames--;

                        continue;
                    }

                    var x = offset.X + s.PX[i];
                    var y = offset.Y + s.PY[i];
                    positions[i] = new Vector2(x, y);
                    trails[COUNT * trails_frame_newest + i] = new Vector2(x, y);
                }
            }

            // RENDER
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                // draw trails
                for (var i = 0; i < trails_frames_count - 1; i++)
                {
                    var f1 = (trails_frame_oldest + i) % TRAIL_LEN_FRAMES;
                    var f2 =                  (f1 + 1) % TRAIL_LEN_FRAMES;

                    // var fi = i + TRAIL_LEN_FRAMES - trails_frames_count; // used for trail brightness
                    var rb = (double)i / trails_frames_count; // relative brightness
                    var v = (byte)(128 * rb);
                    var color = new Color(v, v, v);

                    for (var j = 0; j < COUNT; j++) // for each particle
                    {
                        if (!s.ON[j])
                        {
                            var lo = trails_lo[j]; // number of leftover frames
                            if (lo == 0) continue;

                            var tfc_at_death = trails_td[j];
                            var i_top_margin_horizontal = tfc_at_death - 2  - trails_frame_oldest; // (trails_frames_count + lo) / 2;
                            var i_bot_margin_upward     = tfc_at_death - lo - trails_frame_oldest; // (trails_frames_count - lo) / 2;
                            
                            var skip_particle = trails_frames_count < TRAIL_LEN_FRAMES || tfc_at_death < trails_frames_count
                                //              ^ buffer not full                      OR particle died when buffer wasn't full
                                ? i >= i_top_margin_horizontal || i <= i_bot_margin_upward
                                : i >= lo - 1;
                            if (skip_particle) continue;
                        }

                        var v1 = trails[COUNT * f1 + j]; // [f0 coords] [f1 coords] [...] [fN coords]
                        var v2 = trails[COUNT * f2 + j]; //             \---------\ L = count
                        Raylib.DrawLineEx(v1, v2, 1, color);
                    }
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
                    var j5 = i < 5;
                    var color = new Color(j5 ? 0: 255, 255, j5 ? 0 : blue);
                    Raylib.DrawCircle((int)pos.X, (int)pos.Y, r, color);
                }

                Raylib.DrawText($"{s.ActiveCount}/{COUNT}", 10, 40, 20, Color.Blue);
                Raylib.DrawText($"{trails_frames_count} {trails_frame_oldest}/{trails_frame_newest}", 10, 70, 20, Color.Red);
                // var x = 0;
                // Raylib.DrawText($"{x}: {(s.ON[x] ? "+" : "-")} lo: {trails_lo[x]}/{trails_td[x]}", 10, 100 + 30*x++, 20, Color.White);
                // Raylib.DrawText($"{x}: {(s.ON[x] ? "+" : "-")} lo: {trails_lo[x]}/{trails_td[x]}", 10, 100 + 30*x++, 20, Color.White);
                // Raylib.DrawText($"{x}: {(s.ON[x] ? "+" : "-")} lo: {trails_lo[x]}/{trails_td[x]}", 10, 100 + 30*x++, 20, Color.White);
                // Raylib.DrawText($"{x}: {(s.ON[x] ? "+" : "-")} lo: {trails_lo[x]}/{trails_td[x]}", 10, 100 + 30*x++, 20, Color.White);
                // Raylib.DrawText($"{x}: {(s.ON[x] ? "+" : "-")} lo: {trails_lo[x]}/{trails_td[x]}", 10, 100 + 30*x++, 20, Color.White);
                Raylib.DrawFPS(10, 10);
                Raylib.EndDrawing();
            }
        }
        Raylib.CloseWindow();
    }
}

// todo toggle follow sun to ON ?  offset trails to match sun pos (currently: clear)
// todo drag space ?  offset trails
// todo backspace = pause, launch particles with mouse