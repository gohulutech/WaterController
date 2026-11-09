namespace Model;

public sealed class DeviceType
{
    public int DeviceTypeId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
