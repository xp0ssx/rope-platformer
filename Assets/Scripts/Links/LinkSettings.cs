public readonly struct LinkSettings
{
    public LinkSettings(LinkType type, float lengthMultiplier, float maxForceBeforeBreak)
    {
        Type = type;
        LengthMultiplier = lengthMultiplier;
        MaxForceBeforeBreak = maxForceBeforeBreak;
    }

    public LinkType Type { get; }
    public float LengthMultiplier { get; }
    public float MaxForceBeforeBreak { get; }
}
