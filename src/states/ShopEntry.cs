namespace NewDawn;

/// <summary>Presentation data for a legacy shop list item.</summary>
public class ShopEntry
{
    public string Content { get; set; } = "";
    public object? Tag { get; set; }
    public bool Enabled { get; set; } = true;
}
