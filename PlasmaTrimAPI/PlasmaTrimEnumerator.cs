using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace PlasmaTrimAPI
{
    public static class PlasmaTrimEnumerator
    {

        #region Constants

        /// <summary>
        /// ChromaCove Vendor ID
        /// </summary>
        public static readonly int VendorId = 0x26F3;

        /// <summary>
        /// PlasmaTrim Product ID
        /// </summary>
        public static readonly int ProductId = 0x1000;

        #endregion

        #region Public Methods

        /// <summary>
        /// Locates all connected PlasmaTrim units and returns them.
        /// </summary>
        /// <returns>An enumerable of PlasmaTrim units.</returns>
        public static IEnumerable<PlasmaTrimController> FindConnected()
        {
            // Locate all the connected PlasmaTrim units.
            // var devices = usbDeviceCollection.Where(d => d.ProductId == ProductId && d.VendorId == VendorId);
            UsbDeviceFinder usbDeviceFinder = new UsbDeviceFinder(VendorId, ProductId);

            var usbDevice = UsbDevice.OpenUsbDevice(usbDeviceFinder);

            // New up some PlasmaTrimControllers and send 'em back.
            yield return new PlasmaTrimController(usbDevice);
            //return devices.Select(device => new PlasmaTrimController(device));

        }

        #endregion

    }
}
