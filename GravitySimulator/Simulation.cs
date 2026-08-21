namespace GravitySimulator;

public class Simulation
{
    private const double
        G = 0.25;

    private const float
        VELOCITY_MULTIPLIER = 0.025F;

    private const int
        MAX_MASS_POW = 5,
        NO_MERGE = -1;

    // primary
    public  bool[] ON; // Status
    public float[] PX; // Position
    public float[] PY;
    public float[] VX; // Velocity
    public float[] VY;
    public float[] M;  // Mass

    // derivative
    public   int   ActiveCount;
    public   int   SunIndex;
    public float[] R;  // Radius

    // temporary
    public float[] AX; // Acceleration
    public float[] AY;
    public   int[] MI; // Merge into (index), merge list

    public void Init(int count, float width, float height, bool sun)
    {
        var v = VELOCITY_MULTIPLIER * (sun ? 3 : 2);
        ActiveCount = count;
        PX = InitList_AndFill(count, 0, width);
        PY = InitList_AndFill(count, 0, height);
        VX = InitList_AndFill(count, -v, v);
        VY = InitList_AndFill(count, -v, v);
        AX = new float[count];
        AY = new float[count];
        M  = InitList_AndFill(count, 0, MAX_MASS_POW);
        R  = new float[count];
        ON = new  bool[count];
        MI = new   int[count];

        if (sun) // have 1 massive particle at the center
        {
            PX[0] = width  / 2;
            PY[0] = height / 2;
            VX[0] = VY[0] = 0;
            M[0] = (float)(MAX_MASS_POW + Math.E);
        }

        for (var i = 0; i < count; i++)
        {
            ON[i] = true;
            M [i] = (float)Math.Pow(Math.E, M[i]); // distribute mass exponentially
            R [i] = CalculateRadius(i);
            MI[i] = NO_MERGE;
        }
    }

    private static float[] InitList_AndFill(int count, float min, float max)
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
            if (!ON[i]) continue;

            for (var j = 0; j < count; j++) // account for gravity to all other particles
            {
                if (j == i || !ON[j]) continue;

                AccelerateParticle(i, j);
            }
        }

        // accelerate particles
        for (var i = 0; i < count; i++)
        {
            if (!ON[i]) continue;

            ApplyAcceleration(i);
        }

        // merge particles on the merge list
        var activeCount = ActiveCount;
        for (var i = 0; i < count; i++)
        {
            var tiny = i;
            var huge = MI[tiny];
            if (huge == NO_MERGE) continue;

            MergeParticles(huge, tiny);
        }

        // reset temporary lists
        for (var i = 0; i < count; i++)
        {
            MI[i] = NO_MERGE;
            AX[i] = AY[i] = 0;
        }

        // find biggest particle (if it may have changed)
        if (activeCount != ActiveCount)
        {
            SunIndex = FindSunIndex();
        }
    }

    private int FindSunIndex()
    {
        var max_mass = 0.0F;
        var sun_i = 0;
        var count = M.Length;
        for (var i = 0; i < count; i++)
        {
            if (ON[i] && M[i] > max_mass)
            {
                max_mass = M[i];
                sun_i = i;
            }
        }
        return sun_i;
    }

    private void AccelerateParticle(int i, /* with */ int j)
    {
        var dx = PX[i] - PX[j];
        var dy = PY[i] - PY[j];
        var ri = R[i];
        var rj = R[j];
        var d = Math.Sqrt(dx * dx + dy * dy);
        if (d <= ri + rj) // merge particles
        {
            var huge = ri > rj ? i : j;
            var tiny = ri > rj ? j : i;
            MI[tiny] = huge; // tiny store huge index, so 2+ tiny can be merged into 1 huge
        }
        var m = M[j];
        var ax = G * m * dx / (d * d * d);
        var ay = G * m * dy / (d * d * d);
        AX[i] -= (float)ax;
        AY[i] -= (float)ay;
    }

    private void ApplyAcceleration(int i)
    {
        // affect velocity with acceleration
        VX[i] += AX[i];
        VY[i] += AY[i];
        // affect position with velocity
        PX[i] += VX[i];
        PY[i] += VY[i];
    }

    private void MergeParticles(int huge, int tiny)
    {
        if (!ON[huge]) // huge was already merged into an even bigger one
        {
            while (true) // find first alive in the merge chain instead
            {
                huge = MI[huge];
                if (ON[huge]) break; // NO_MERGE -> IOB
            }
        }

        // mass
        var m1 = M[huge];
        var m2 = M[tiny];
        var mN = m1 + m2;
        // Combine mass, update radius
        M[huge] = mN;
        R[huge] = CalculateRadius(huge);

        // velocity
        var vx1 = VX[huge];
        var vy1 = VY[huge];
        var vx2 = VX[tiny];
        var vy2 = VY[tiny];
        // Conserve momentum
        VX[huge] = (m1 * vx1 + m2 * vx2) / mN;
        VY[huge] = (m1 * vy1 + m2 * vy2) / mN;

        // position
        var x1 = PX[huge];
        var y1 = PY[huge];
        var x2 = PX[tiny];
        var y2 = PY[tiny];
        // Put particle into center of mass
        PX[huge] = (m1 * x1 + m2 * x2) / mN;
        PY[huge] = (m1 * y1 + m2 * y2) / mN;

        ON[tiny] = false;
        ActiveCount--;
    }

    private float CalculateRadius(int i)
    {
        return (float)Math.Pow(0.75 * M[i] / Math.PI, 1 / 3.0);
    }
}