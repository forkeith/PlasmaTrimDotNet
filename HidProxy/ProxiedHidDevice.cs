using HidSharp;

namespace HidProxy;

// https://forum.zer7.com/topic/10158/
public class ProxiedHidDevice(HidDevice Device) : IHidDevice
{
    public Stream Open()
    {
        return Device.Open();
    }

    public int VendorID => Device.VendorID;
    public int ProductID => Device.ProductID;
}
