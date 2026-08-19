namespace GravitySimulator;

public class Simulation
{
    public const double G = 1; // 6.6743E-11 too small lol))
    public const int MAX_MASS_POW = 5;

    public float[] PX; // Position
    public float[] PY;
    public float[] VX; // Velocity
    public float[] VY;
    public float[] AX; // Acceleration
    public float[] AY;
    public float[] R; // Radius
    public float[] M; // Mass

    public void Init(int count, float width, float height, bool sun)
    {
        var vmax = sun ? 3 : 1;
        PX = InitList(count, 0, width);
        PY = InitList(count, 0, height);
        VX = InitList(count, -vmax, vmax);
        VY = InitList(count, -vmax, vmax);
        AX = new float[count];
        AY = new float[count];
        M  = InitList(count, 0, MAX_MASS_POW);
        R  = new float[count];

        if (sun) // have 1 massive particle at the center
        {
            PX[0] = width  / 2;
            PY[0] = height / 2;
            VX[0] = VY[0] = 0;
            M[0] = (float)(MAX_MASS_POW + Math.E);
        }

        for (var i = 0; i < count; i++) // distribute mass exponentially
        {
            M[i] = (float)Math.Pow(Math.E, M[i]);
        }

        for (int i = 0; i < count; i++) // calculate radii
        {
            R[i] = (float)Math.Pow(0.75 * M[i] / Math.PI, 1 / 3.0);
        }
    }

    private static float[] InitList(int count, float min, float max)
    {
        var list = new float[count];
        for (var i = 0; i < count; i++)
        {
            list[i] = min + Random.Shared.NextSingle() * (max - min);
        }

        return list;
    }

    public void Tick()
    {
        var count = M.Length;
        
        // calculate acceleration
        for (var i = 0; i < count; i++) // for each particle
        {
            AX[i] = AY[i] = 0;
            for (var j = 0; j < count; j++) // account for gravity to all other particles
            {
                if (j == i) continue;

                var dx = PX[i] - PX[j];
                var dy = PY[i] - PY[j];
                var d = Math.Sqrt(dx * dx + dy * dy);
                var ax = G * M[j] * dx / (d * d * d);
                var ay = G * M[j] * dy / (d * d * d);
                AX[i] -= (float)ax;
                AY[i] -= (float)ay;
            }
        }

        for (int i = 0; i < count; i++)
        {
            // affect velocity with acceleration
            VX[i] += AX[i];
            VY[i] += AY[i];
            // affect position with velocity
            PX[i] += VX[i];
            PY[i] += VY[i];
        }
    }
}