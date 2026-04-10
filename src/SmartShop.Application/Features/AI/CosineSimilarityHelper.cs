namespace SmartShop.Application.Features.AI;

internal static class CosineSimilarityHelper
{
    /// <summary>
    /// Trả về -1 nếu hai vector khác số chiều (embedding cũ, cần regenerate).
    /// </summary>
    public static double Compute(float[] a, float[] b)
    {
        if (a.Length != b.Length) return -1;

        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot  += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return magA == 0 || magB == 0 ? 0 : dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
