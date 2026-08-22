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

    // game state
    private readonly Simulation s = new();
    private bool debug = true;
    private bool pause;
    private bool step_once;
    private bool follow_sun = SUN && FOLLOW_SUN_DEFAULT;
    private bool sun_space_trails = false;
    private Vector2 offset;
    private readonly Vector2[] positions = new Vector2[COUNT]; // (for convenience)

    // trails
    private readonly Vector2[] trails    = new Vector2[COUNT * TRAIL_LEN_FRAMES];
    private readonly int[] trails_lo = new  int[COUNT]; // leftover frames counts for dead particles: 600 -> 0
    private readonly int[] trails_td = new  int[COUNT]; // tfc value at particle death
    private int trails_frame_newest; // n
    private int trails_frame_oldest; // o
    private int trails_frames_count; // tfc

    // temporary
    private Vector2 screen_center;

    private void StartSimulation()
    {
        s.Init(COUNT, W, H, SUN);
    }

    private void ResetTrails()
    {
        Array.Clear(trails);
        Array.Fill(trails_lo, -1);
        Array.Fill(trails_td, -1);
        trails_frame_newest = -1;
        trails_frame_oldest = 0;
        trails_frames_count = 0;
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

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            DrawTrails();
            DrawParticles();
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
            follow_sun = false;
            offset += Raylib.GetMouseDelta();
        }
    }

    private void DoLogic()
    {
        s.Tick();

        if (follow_sun)
        {
            var sun = s.SunIndex;
            offset.X = screen_center.X - s.PX[sun];
            offset.Y = screen_center.Y - s.PY[sun];
        }

        // manage trail frames
        {
            trails_frame_newest = (trails_frame_newest + 1) % TRAIL_LEN_FRAMES;

            if (trails_frames_count == TRAIL_LEN_FRAMES)
                trails_frame_oldest = (trails_frame_oldest + 1) % TRAIL_LEN_FRAMES;
            else
                trails_frames_count++;
        }

        for (var i = 0; i < COUNT; i++)
        {
            if (!s.ON[i]) // manage dead particles trails
            {
                ref var
                    leftover_frames = ref trails_lo[i];
                if (leftover_frames == -1)
                    leftover_frames = trails_td[i] = trails_frames_count;
                if (leftover_frames > 0 && trails_frames_count == TRAIL_LEN_FRAMES)
                    leftover_frames--;

                continue;
            }

            // store particle position for trails
            var x = s.PX[i];
            var y = s.PY[i];
            positions[i] = trails[COUNT * trails_frame_newest + i] = new Vector2(x, y);
        }
    }

    private void DrawTrails()
    {
        for (var i = 0; i < trails_frames_count - 1; i++)
        {
            var f1 = (trails_frame_oldest + i) % TRAIL_LEN_FRAMES;
            var f2 =                  (f1 + 1) % TRAIL_LEN_FRAMES;

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
                    var skip_particle = trails_frames_count < TRAIL_LEN_FRAMES || tfc_at_death < trails_frames_count
                        //              ^ buffer not full                      OR particle died when buffer wasn't full
                        ? i >= tfc_at_death - 2 - trails_frame_oldest
                        : i >= lo - 1;
                    if (skip_particle) continue;
                }

                var offset1 = sun_space_trails
                    ? offset + positions[s.SunIndex] - trails[COUNT * f1 + s.SunIndex]
                    : offset;
                var offset2 = sun_space_trails
                    ? offset + positions[s.SunIndex] - trails[COUNT * f2 + s.SunIndex]
                    : offset;
                var v1 = offset1 + trails[COUNT * f1 + j]; // [f0 coords] [f1 coords] [...] [fN coords]
                var v2 = offset2 + trails[COUNT * f2 + j]; //             \---------\ L = count
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

    private void DrawTextOverlays()
    {
        const string
            help1 = "Particles: [alive / total]",
            help2 = "Trail frames: [count / max count] [oldest / newest]",
            helpK =  "Esc - Quit"
                + " | F3 - Debug"
                + " | Enter - Restart"
                + " | Backspace - Pause"
                + " | = - Step (when paused)"
                + " | \\ - Follow the Sun"
                + " | S - Sun-space trails"
                + " | M1 - Drag space";

        var h = Raylib.GetScreenHeight();

        if (debug)
        {
            var row = 0;

            Raylib.DrawFPS(10, TextHeight());
            Raylib.DrawText($"{s.ActiveCount}/{COUNT}",                     10, TextHeight(), 20, Color.Blue);
            Raylib.DrawText($"{trails_frames_count}/{TRAIL_LEN_FRAMES}",    10, TextHeight(), 20, Color.Red);
            Raylib.DrawText($"{trails_frame_oldest}/{trails_frame_newest}", 10, TextHeight(), 20, Color.Red);
            Raylib.DrawText(help1,                                          10,       h - 40, 10, Color.Blue);
            Raylib.DrawText(help2,                                         150,       h - 40, 10, Color.Red);

            int TextHeight() => 10 + 30 * row++;
        }
        Raylib.DrawText(helpK, posX: 10, posY: h - 20, fontSize: 10, Color.LightGray);
    }
}

// todo launch particles with mouse
// todo paused game - update positions on restart / sun focus toggle