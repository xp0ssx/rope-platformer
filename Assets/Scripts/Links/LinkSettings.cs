public readonly struct LinkSettings
{
    public LinkSettings(LinkType type)
    {
        Type = type;
    }

    public LinkType Type { get; }
}
