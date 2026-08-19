namespace GravitySimulator;

public class Simulation
{
    public const double G = 1; // 6.6743E-11 too small lol))
    public const int MAX_MASS_POW = 5;

    public float[] PX;
    public float[] PY;
    public float[] VX;
    public float[] VY;
    public float[] AX;
    public float[] AY;
    public float[] Mass;

    public void Init(int count, float width, float height, bool sun)
    {
        var vmax = sun ? 3 : 1;
        PX = InitList(count, 0, width);
        PY = InitList(count, 0, height);
        VX = InitList(count, -vmax, vmax);
        VY = InitList(count, -vmax, vmax);
        AX = new float[count];
        AY = new float[count];
        Mass = InitList(count, 0, MAX_MASS_POW);

        if (sun) // have 1 massive particle at the center
        {
            PX[0] = width  / 2;
            PY[0] = height / 2;
            VX[0] = VY[0] = 0;
            Mass[0] = (float)(MAX_MASS_POW + Math.E);
        }

        for (var i = 0; i < count; i++) // distribute mass exponentially
        {
            Mass[i] = (float)Math.Pow(Math.E, Mass[i]);
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
        var count = Mass.Length;
        
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
                var ax = G * Mass[j] * dx / (d * d * d);
                var ay = G * Mass[j] * dy / (d * d * d);
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

    public double GetParticleRadius(int i, float multiplier)
    {
        return multiplier * Math.Pow(0.75 * Mass[i] / Math.PI, 1 / 3.0);
    }
}