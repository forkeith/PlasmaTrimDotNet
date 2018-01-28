using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;

namespace PlasmaTrimAPI
{
    public class PlasmaTrimController
    {

        #region Properties

        /// <summary>
        /// The serial number of this PlasmaTrim device.
        /// </summary>
        public string SerialNumber { get; private set; }

        /// <summary>
        /// The device handle.
        /// </summary>
        private HidDevice Device { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new PlasmaTrimController object from an HidDevice object.
        /// </summary>
        /// <param name="device">An HidDevice object that is a reference to a PlasmaTrimController</param>
        public PlasmaTrimController(HidDevice device)
        {

            // First, let's do a sanity check, in case someone tried to pass a weird device in here.
            if (device.Attributes.VendorId != PlasmaTrimEnumerator.VendorId || device.Attributes.ProductId != PlasmaTrimEnumerator.ProductId)
                throw new ArgumentException("Provided device could not be identified as a PlasmaTrim!");

            // Store the reference to the device.
            this.Device = device;

            // Populate the device serial number, so that this unit can be identified.
            this.GetDeviceDetails();

        }
       
        #endregion

        #region Public Methods

        /// <summary>
        /// Opens a connection to the PlasmaTrim device.
        /// </summary>
        public void OpenDevice()
        {

            // Pretty self-explanatory.
            this.Device.OpenDevice();

        }

        /// <summary>
        /// Closes the connection to the PlasmaTrim device.
        /// </summary>
        public void CloseDevice()
        {

            // Also self-explanatory.
            if (this.Device.IsOpen)
                this.Device.CloseDevice();

        }

        /// <summary>
        /// Play the sequence stored in device memory.
        /// </summary>
        public void PlayStoredSequence()
        {
            this.SendCommand(PlasmaTrimCommand.PlayStoredSequence);
        }

        /// <summary>
        /// Stop playing the sequence stored in device memory.
        /// </summary>
        public void StopStoredSequence()
        {
            this.SendCommand(PlasmaTrimCommand.StopStoredSequence);
        }

        /// <summary>
        /// Sets all LED colors immediately, without storing the change.
        /// </summary>
        public void SetColorsImmediate(Color[] colors, byte brightness)
        {

            // Sanity check!
            if (colors == null || colors.Length != 8)
                throw new ArgumentException("Color array must contain 8 elements!");

            // Set up our command buffer
            byte[] data = new byte[25];

            // Fill it with colors!
            var colorIndex = 0;

            // Populate the color arguments
            foreach (var color in colors)
            {
                data[colorIndex++] = color.R;
                data[colorIndex++] = color.G;
                data[colorIndex++] = color.B;
            }

            // Limit the brightness and set it.
            if (brightness > 0x64) brightness = 0x64;
            data[colorIndex] = brightness;

            // Finally, actually execute the command.
            this.SendCommand(PlasmaTrimCommand.SetColorImmediate, data);

        }

        /// <summary>
        /// Gets the current color state of all LEDs on the device.
        /// </summary>
        /// <returns>An array of the color assignments of the device's LEDs.</returns>
        public Color[] GetColorsImmediate()
        {
            // Get color data from the device.
            var data = this.QueryDevice(PlasmaTrimCommand.GetColorImmediate);
            Color[] colors = new Color[8];

            // Build color objects.
            for (var i = 0; i < 8; i++)
            {
                // Extract RGB data from this LED position
                var R = data[(3 * i) + 2];
                var G = data[(3 * i) + 3];
                var B = data[(3 * i) + 4];

                // Create a color.
                colors[i] = Color.FromArgb(R, G, B);
            }

            return colors;
        }

        /// <summary>
        /// Sets all LED colors immediately, without storing the change.
        /// </summary>
        public void PulseColor(Color color)
        {
            var colors = GetArrayOfColor(color);

            // Pulse the color
            for (byte brightness = 0x63; brightness > 0; brightness -= 3)
            {
                // Set the color and brightness.
                this.SetColorsImmediate(colors, brightness);

                // TODO: Not this.
                Thread.Sleep(15);
            }

            // Return the device to maximum brightness.
            // TODO: SetBrightnessImmediate method!
            this.SetColorsImmediate(colors, 0x63);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets this device's serial number attribute so that it can be identified.
        /// </summary>
        private void GetDeviceDetails()
        {

            // Open a connection to the device
            this.OpenDevice();

            // Get the device's serial number.
            var deviceInfo = this.QueryDevice(PlasmaTrimCommand.GetSerialNumber);

            // Save the serial on this object
            this.SerialNumber = BitConverter.ToString(new ArraySegment<byte>(deviceInfo, 1, 4).Reverse().ToArray());

            // Close the connection for now.
            this.CloseDevice();

        }

        /// <summary>
        /// Executes a command against the PlasmaTrim device.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="data">The data payload.</param>
        private void SendCommand(PlasmaTrimCommand command, byte[] data = null)
        {

            // Make sure we're connected to the device.
            if (!this.Device.IsOpen)
                throw new InvalidOperationException("PlasmaTrim device is not connected!");

            byte[] commandData = new byte[33];

            // Index 1 is the command we want to send.
            commandData[1] = (byte)command;

            // Copy the data into our command array.
            if (data != null) data.CopyTo(commandData, 2);

            // Actually send the command.
            if (!this.Device.Write(commandData))
                throw new Exception("Unable to send command to PlasmaTrim device!");

        }

        /// <summary>
        /// Executes a command and then returns the returned data.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="data">The data payload.</param>
        ///         /// <returns>The output of the command</returns>
        private byte[] QueryDevice(PlasmaTrimCommand command, byte[] data = null)
        {
            // Send the command to the device.
            SendCommand(command, data);

            // Now, query the device for output and return it.
            return this.Device.ReadReport(1).Data;
        }

        /// <summary>
        /// Gets an array of one color.
        /// </summary>
        /// <param name="color">The color to project across the array</param>
        /// <returns>A populated array.</returns>
        private Color[] GetArrayOfColor(Color color)
        {
            Color[] colors = new Color[8];

            for (var i = 0; i < 8; i++)
                colors[i] = color;

            return colors;
        }

        #endregion

    }

    public enum PlasmaTrimCommand : byte
    {
        SetColorImmediate = 0x00,
        GetColorImmediate = 0x01,
        PlayStoredSequence = 0x02,
        StopStoredSequence = 0x03,
        SetSequenceLength = 0x04,
        GetSequenceLength = 0x05,
        SetSequenceStep = 0x06,
        GetSequenceStep = 0x07,
        SetDeviceName = 0x08,
        GetDeviceName = 0x09,
        GetSerialNumber = 0x0A,
        SetBrightness = 0x0B,
        GetBrightness = 0x0C
    }


}
