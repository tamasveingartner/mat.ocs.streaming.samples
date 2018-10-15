namespace MAT.OCS.Streaming.Samples.CSharp
{
    internal class NumberToBarString
    {
        public static string Convert(double number) => new string('.', (int)(50 * number));
    }
}