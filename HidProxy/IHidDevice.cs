namespace HidProxy;

public interface IHidDevice
{
    Stream Open();
    int ProductID { get; }
    int VendorID { get; }
}
