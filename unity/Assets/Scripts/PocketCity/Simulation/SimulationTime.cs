namespace PocketCity.Simulation
{
    public interface ITimeProvider
    {
        float CurrentTime { get; }
    }

    public class UnityTimeProvider : ITimeProvider
    {
        public float CurrentTime => UnityEngine.Time.time;
    }

    public static class SimulationTime
    {
        private static ITimeProvider timeProvider = new UnityTimeProvider();

        public static float Time => timeProvider.CurrentTime;

        public static void SetProvider(ITimeProvider provider)
        {
            timeProvider = provider ?? new UnityTimeProvider();
        }
    }
}
