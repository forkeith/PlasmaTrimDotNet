using FluentAssertions;
using Moq;
using PlasmaTrimAPI;
using HidProxy;

namespace PlasmaTrimDotNet;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var hidDeviceFake = new FakeHidDevice(PlasmaTrimEnumerator.VendorId, PlasmaTrimEnumerator.ProductId);
        
        var hidStreamFake = hidDeviceFake.GetFakeStream();
        hidStreamFake.MockStreamResponse(req => req[1] == (byte)PlasmaTrimCommand.GetSerialNumber ? "000A0959B80001011E000000000000000000000000000000000000000000000000" : null);
        hidStreamFake.MockStreamResponse(req => req[1] == (byte)PlasmaTrimCommand.GetDeviceName ? "0009506C61736D615472696D205247422D38203078423835393039000000000000" : null);

        var sut = new PlasmaTrimController(hidDeviceFake);

        // Assert
        sut.SerialNumber.Should().Be("00-B8-59-09");
        sut.Name.Should().Be("PlasmaTrim RGB-8 0xB85909");
    }
}
