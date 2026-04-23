namespace System.Diagnostics;

public static class StopwatchExtensions
{
    public static TimeSpan StopAndReturnEllapsed(this Stopwatch st)
    {
        st.Stop();
        return st.Elapsed;
    }
}
