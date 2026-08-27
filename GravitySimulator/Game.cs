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
        RADIUS_MULTIPLIER = 1.0F,
        NEW_PARTICLE_V_MULT = 0.1F;

    private static readonly Vector2
        V2_INVALID = new(-1, -1);

    // game state
    private readonly Simulation s = new();
    private bool debug = true;
    private bool pause;
    private bool step_once;
    private bool follow_sun = SUN && FOLLOW_SUN_DEFAULT;
    private bool sun_space_trails = false;
    private bool launch_mode = false;
    private Vector2 offset;
    private readonly Vector2[] positions = new Vector2[COUNT]; // (for convenience)

    // trails
    private readonly Vector2[] trails = new Vector2[COUNT * TRAIL_LEN_FRAMES]; // [p0 frames][p1 frames][…]
    private readonly int[] trails_off = new     int[COUNT]; // trail offset (first frame)
    private readonly int[] trails_len = new     int[COUNT]; // trail length
    private readonly int[] trails_mvf = new     int[COUNT]; // max visible frames (ON 600, OFF 599->0)
    private int dbg_trail = 0; // index of particle to debug trail

    // launching particle
    private Vector2 drag_start = V2_INVALID;
    private Vector2 drag;
    private Vector2 new_pos = V2_INVALID;

    // temporary
    private Vector2 screen_center;

    private void StartSimulation()
    {
        s.Init(COUNT, W, H, SUN);
    }

    private void ResetTrails()
    {
        Array.Clear(trails);
        Array.Clear(trails_off);
        Array.Clear(trails_len);
        Array.Fill(trails_mvf, TRAIL_LEN_FRAMES);
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(W, H, "Gravity Simulator");
        Raylib.SetTargetFPS(FPS);

        StartSimulation();
        ResetTrails();

        while (!Raylib.WindowShouldClose())
        {
            HandleInput();

            if (!pause || step_once) DoLogic();
            else                     DoLogic_Pause();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            DrawTrails();
            DrawParticles();
            if (drag_start != V2_INVALID) DrawLaunch();
            DrawTextOverlays();
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    private void HandleToggle(KeyboardKey key, ref bool field)
    {
        var toggle = Raylib.IsKeyPressed(key);
        if (toggle) field = !field;
    }

    private void HandleInput()
    {
        screen_center = Raylib.GetScreenCenter();

        HandleToggle(KeyboardKey.F3,           ref debug);
        HandleToggle(KeyboardKey.Backspace,    ref pause);
        HandleToggle(KeyboardKey.Backslash,    ref follow_sun);
        HandleToggle(KeyboardKey.S,            ref sun_space_trails);
        HandleToggle(KeyboardKey.D,            ref launch_mode);
        
        step_once = pause && Raylib.IsKeyPressed(KeyboardKey.Equal);

        var restart = Raylib.IsKeyPressed(KeyboardKey.Enter);
        if (restart)
        {
            StartSimulation();
            ResetTrails();
            offset = new Vector2();
        }

        var mouse_down = Raylib.IsMouseButtonDown(MouseButton.Left);
        if (mouse_down)
        {
            if (launch_mode)
            {
                if (drag_start == V2_INVALID)
                {
                    drag_start = Raylib.GetMousePosition();
                    drag = Vector2.Zero;
                }

                drag += Raylib.GetMouseDelta();
            }
            else // drag space
            {
                follow_sun = false;
                offset += Raylib.GetMouseDelta();
            }
        }
        else
        {
            if (drag_start != V2_INVALID)
            {
                new_pos = drag_start - offset;
                drag_start = V2_INVALID;
            }
        }
    }

    private void DoLogic_Pause()
    {
        HandleParticleLaunch();

        for (var i = 0; i < COUNT; i++)
        {
            var x = s.PX[i];
            var y = s.PY[i];
            positions[i] = new Vector2(x, y);
        }

        if (follow_sun) FollowSun();
    }

    private void DoLogic()
    {
        HandleParticleLaunch();

        s.Tick();

        if (follow_sun) FollowSun();

        for (var i = 0; i < COUNT; i++)
        {
            ref var len = ref trails_len[i];
            ref var off = ref trails_off[i];
            ref var mvf = ref trails_mvf[i];
            if (s.ON[i])
            {
                // store particle position for trails
                var x = s.PX[i];
                var y = s.PY[i];
                positions[i] = trails[i * TRAIL_LEN_FRAMES + (off + len) % TRAIL_LEN_FRAMES] = new Vector2(x, y);

                if (len < TRAIL_LEN_FRAMES)
                    len++;
                else
                    off = (off + 1) % TRAIL_LEN_FRAMES;
            }
            else // OFF
            {
                if (mvf > 0)
                    mvf--;
            }
        }
    }

    private void HandleParticleLaunch()
    {
        if (new_pos != V2_INVALID && s.ActiveCount < COUNT)
        {
            var velocity = drag * NEW_PARTICLE_V_MULT;
            var i = s.LaunchParticle(new_pos.X, new_pos.Y, velocity.X, velocity.Y, 200);
            dbg_trail = i;
            trails_off[i] = (trails_off[s.SunIndex] + trails_len[s.SunIndex]) % TRAIL_LEN_FRAMES;
            trails_len[i] = 0; // ^ sync with sun
            trails_mvf[i] = TRAIL_LEN_FRAMES;
            new_pos = V2_INVALID;
        }
    }

    private void FollowSun()
    {
        var sun = s.SunIndex;
        offset.X = screen_center.X - s.PX[sun];
        offset.Y = screen_center.Y - s.PY[sun];
    }

    private void DrawTrails()
    {
        var sun = s.SunIndex;

        for (var i = 0; i < COUNT; i++) // for each particle
        {
            var off = trails_off[i];
            var len = trails_len[i];
            var mvf = trails_mvf[i];

            if (len < 2 || mvf <= 0) continue;

            var f0 = Math.Max(0, len - mvf);
            for (var f = f0; f < len - 1; f++) // for each visible frame
            {
                var f1 = (off + f)     % TRAIL_LEN_FRAMES;
                var f2 = (off + f + 1) % TRAIL_LEN_FRAMES;

                var offset1 = offset;
                var offset2 = offset;
                if (sun_space_trails)
                {
                    offset1 += positions[sun] - trails[sun * TRAIL_LEN_FRAMES + f1];
                    offset2 += positions[sun] - trails[sun * TRAIL_LEN_FRAMES + f2];
                }

                var v1 = offset1 + trails[i * TRAIL_LEN_FRAMES + f1];
                var v2 = offset2 + trails[i * TRAIL_LEN_FRAMES + f2];

                var mb = len + TRAIL_LEN_FRAMES - mvf; // max brightness
                var rb = (double)(f - f0) / mb;   // relative brightness
                const byte c = 255;
                var color = new Color(c, c, c, (byte)(128 * rb));

                Raylib.DrawLineEx(v1, v2, 1, color);
            }
        }
    }

    private void DrawParticles()
    {
        for (var i = 0; i < COUNT; i++)
        {
            if (!s.ON[i]) continue;

            var pos = offset + positions[i];
            var r = s.R[i] * RADIUS_MULTIPLIER;
            var m = s.M[i];
            var v = m < 1024
                ? 1.0
                : 1.0 - Math.Log(m / 1024.0) / Math.Log(32.0); // v(1k) = 1  v(32k) = 0
            var blue = (int)(255 * Math.Clamp(v, 0, 1));
            var color = new Color(255, 255, blue);
            Raylib.DrawCircle((int)pos.X, (int)pos.Y, r, color);
        }
    }

    private void DrawLaunch()
    {
        Raylib.DrawCircleV(drag_start, 5, Color.Green);
        Raylib.DrawLineEx(drag_start, drag_start + drag, 1, Color.Green);
    }

    private void DrawTextOverlays()
    {
        const string
            help1 = "Particles: [alive / total]",
            help2 = "Trail frames: [particle index: max visible] [offset + count / max count]",
            helpK =  "Esc - Quit"
                + " | F3 - Debug"
                + " | Enter - Restart"
                + " | Backspace - Pause"
                + " | = - Step (when paused)"
                + " | \\ - Follow the Sun"
                + " | S - Sun-space trails"
                + " | D - Launch mode"
                + " | M1 - Drag space / launch particle";

        var h = Raylib.GetScreenHeight();

        if (debug)
        {
            var row = 0;
            var i = dbg_trail;

            Raylib.DrawFPS(10, TextHeight());
            Raylib.DrawText($"{s.ActiveCount}/{COUNT}",                            10, TextHeight(), 20, Color.Blue);
            Raylib.DrawText($"{i}: {trails_mvf[i]}",                               10, TextHeight(), 20, Color.Red);
            Raylib.DrawText($"{trails_off[i]}+{trails_len[i]}/{TRAIL_LEN_FRAMES}", 10, TextHeight(), 20, Color.Red);
            Raylib.DrawText(help1,                                                 10,       h - 40, 10, Color.Blue);
            Raylib.DrawText(help2,                                                150,       h - 40, 10, Color.Red);

            int TextHeight() => 10 + 30 * row++;
        }
        Raylib.DrawText(helpK, posX: 10, posY: h - 20, fontSize: 10, Color.LightGray);
    }
}