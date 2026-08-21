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

    public   int   ActiveCount;
    public   int   SunIndex;
    public  bool[] ON; // Status
    public float[] PX; // Position
    public float[] PY;
    public float[] VX; // Velocity
    public float[] VY;
    public float[] AX; // Acceleration
    public float[] AY;
    public float[] R;  // Radius
    public float[] M;  // Mass
    public   int[] MI; // Merge into (index), merge list

    public void Init(int count, float width, float height, bool sun)
    {
        var vmax = VELOCITY_MULTIPLIER * (sun ? 3 : 2);
        ActiveCount = count;
        PX = InitList(count, 0, width);
        PY = InitList(count, 0, height);
        VX = InitList(count, -vmax, vmax);
        VY = InitList(count, -vmax, vmax);
        AX = new float[count];
        AY = new float[count];
        M  = InitList(count, 0, MAX_MASS_POW);
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
            if (!ON[i]) continue;

            AX[i] = AY[i] = 0;
            for (var j = 0; j < count; j++) // account for gravity to all other particles
            {
                if (j == i || !ON[j]) continue;

                var dx = PX[i] - PX[j];
                var dy = PY[i] - PY[j];
                var d = Math.Sqrt(dx * dx + dy * dy);
                var ri = R[i];
                var rj = R[j];
                if (d <= ri + rj) // add to merge list
                {
                    var huge  = ri > rj ? i : j;
                    var tiny = ri > rj ? j : i;
                    MI[tiny] = huge; // tiny store huge index, so 2+ tiny can be merged into 1 huge
                    // todo fix tiny particle on big speed can escape merge by phasing thru huge one
                }
                var ax = G * M[j] * dx / (d * d * d);
                var ay = G * M[j] * dy / (d * d * d);
                AX[i] -= (float)ax;
                AY[i] -= (float)ay;
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (!ON[i]) continue;

            // affect velocity with acceleration
            VX[i] += AX[i];
            VY[i] += AY[i];
            // affect position with velocity
            PX[i] += VX[i];
            PY[i] += VY[i];
        }

        // merge particles on merge list
        // tiny one stores huge index!
        var activeCount = ActiveCount;
        for (var i = 0; i < count; i++)
        {
            var tiny = i;
            var huge = MI[tiny];

            if (huge == NO_MERGE) continue;

            if (!ON[huge]) // huge was merged into a REALLY HUGE
            {
                // merge tiny into the REALLY HUGE instead/
                // or even better - into first in chain that is still alive
                while (true)
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

        // clear merge list
        for (var i = 0; i < count; i++)
        {
            MI[i] = -1;
        }

        if (activeCount != ActiveCount)
        {
            SunIndex = FindSunIndex();
        }
    }

    private float CalculateRadius(int i)
    {
        return (float)Math.Pow(0.75 * M[i] / Math.PI, 1 / 3.0);
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
}