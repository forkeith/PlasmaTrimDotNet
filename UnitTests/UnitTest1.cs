using Moq;
using PlasmaTrimAPI;
using HidProxy;

namespace PlasmaTrimDotNet;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var hidDeviceMock = new Mock<IHidDevice>();
        var hidStreamMock = new Mock<Stream>();

        hidDeviceMock.SetupGet(x => x.VendorID).Returns(PlasmaTrimEnumerator.VendorId);
        hidDeviceMock.SetupGet(x => x.ProductID).Returns(PlasmaTrimEnumerator.ProductId);

        hidDeviceMock.Setup(x => x.Open()).Returns(hidStreamMock.Object);
        MockStreamResponse(hidStreamMock, PlasmaTrimCommand.GetSerialNumber, "000A0959B80001011E000000000000000000000000000000000000000000000000");

        var sut = new PlasmaTrimController(hidDeviceMock.Object);

    }

    private static void MockStreamResponse(Mock<Stream> hidStreamMock, PlasmaTrimCommand command, string sendHexResponse)
    {
        hidStreamMock.Setup(x => x.Write(It.IsAny<byte[]>())).Callback<byte[]>(request => {
            if (request[1] == (int)command) {
                var response = Convert.FromHexString(sendHexResponse);
                hidStreamMock.Setup(x => x.Read(It.IsAny<byte[]>())).Returns(response.Length).Callback<byte[]>(responseBytes => {
                    //response.CopyTo(responseBytes);
                    Array.Copy(response, responseBytes, response.Length);
                    //return response.Length;
                });
            }
        });
    }
}
