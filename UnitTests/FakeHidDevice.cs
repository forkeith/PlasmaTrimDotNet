using HidProxy;

public class FakeHidDevice : IHidDevice
{
    private FakeHidStream _stream = new FakeHidStream();
    
    public FakeHidDevice(int vendorId, int productId)
    {
        VendorID = vendorId;
        ProductID = productId;
    }

    public Stream Open()
    {
        return _stream;
    }

    public FakeHidStream GetFakeStream() => _stream;

    public int ProductID { get; init; }
    public int VendorID { get; init; }
}
