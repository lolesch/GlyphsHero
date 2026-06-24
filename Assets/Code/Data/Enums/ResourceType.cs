namespace Code.Data.Enums
{
    /// <summary>
    /// The pool a weapon spends from (Cost) or an on-hit effect restores to (Gain).
    /// Cost and Gain name this independently — a mana-cost weapon can leech health (ADR-0005).
    /// The Converter reclassifies the Cost pool via <c>ConverterAxis.Resource</c>.
    /// </summary>
    public enum ResourceType
    {
        Mana,
        Health,
    }
}
