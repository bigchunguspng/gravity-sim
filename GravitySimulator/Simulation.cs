namespace GravitySimulator;

public class Simulation
{
    public const double G = 1; // 6.6743E-11 too small lol))

    public float[] PX;
    public float[] PY;
    public float[] VX;
    public float[] VY;
    public float[] AX;
    public float[] AY;
    public float[] Mass;

    public void Init(int count, float width, float height)
    {
        PX = InitList(count, 0, width);
        PY = InitList(count, 0, height);
        VX = InitList(count, 0, 0);
        VY = InitList(count, 0, 0);
        AX = new float[count];
        AY = new float[count];
        Mass = InitList(count, 1, 10);
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
}