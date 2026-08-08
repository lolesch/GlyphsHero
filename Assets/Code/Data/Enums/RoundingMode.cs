namespace Code.Data.Enums
{
    /// <summary>How a <see cref="Code.Runtime.Modules.Statistics.MutableInt"/> converts its blended
    /// float total to the int it reports.</summary>
    public enum RoundingMode : byte
    {
        Nearest = 0,
        Floor = 1,
        Ceil = 2,
    }
}
