namespace Core.Shared.Entities;

public class SelectOption
{
    public string Label { get; set; } = string.Empty;
    public object? Value { get; set; }
    public object? Ext { get; set; }
}
