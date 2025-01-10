using HidSharp;

namespace HidProxy;

public class ProxiedHidDevice(HidDevice Device) : IHidDevice
{
    public Stream Open()
    {
        return Device.Open();
    }

    public int VendorID => Device.VendorID;
    public int ProductID => Device.ProductID;
}
