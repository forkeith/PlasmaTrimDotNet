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
        /// The configurable name of this PlasmaTrim device.
        /// </summary>
        public string Name { get; private set; }

        public const byte MaxBrightness = 0x64;
        public const int LedCount = 8;
        public const int MaxSequenceSteps = 76;

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
                throw new ArgumentException("Provided device could not be identified as a PlasmaTrim!", nameof(device));

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
            this.QueryDevice(PlasmaTrimCommand.PlayStoredSequence);
        }

        /// <summary>
        /// Stop playing the sequence stored in device memory.
        /// </summary>
        public void StopStoredSequence()
        {
            this.QueryDevice(PlasmaTrimCommand.StopStoredSequence);
        }

        /// <summary>
        /// Sets all LED colors immediately, without storing the change.
        /// </summary>
        public void SetColorsImmediate(Color[] colors, byte brightness)
        {

            // Sanity check!
            if (colors == null || colors.Length != LedCount)
                throw new ArgumentException($"Color array must contain {LedCount} elements!", nameof(colors));

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
            if (brightness > MaxBrightness) brightness = MaxBrightness;
            data[colorIndex] = brightness;

            // Finally, actually execute the command.
            this.QueryDevice(PlasmaTrimCommand.SetColorImmediate, data);

        }

        /// <summary>
        /// Gets the current color state of all LEDs on the device.
        /// </summary>
        /// <returns>An array of the color assignments of the device's LEDs.</returns>
        public Color[] GetColorsImmediate()
        {
            // Get color data from the device.
            var data = this.QueryDevice(PlasmaTrimCommand.GetColorImmediate);
            var colors = GetColorsImpl(data, 2);

            return colors;
        }

        /// <summary>
        /// Set the color and the brightness to full, and slowly dim it.
        /// </summary>
        public async Task PulseColor(Color color)
        {
            var colors = GetArrayOfColor(color);
            this.SetColorsImmediate(colors, MaxBrightness);

            // Pulse the color from 99% to 0% brightness in 3% decrements
            // sleeping for 15 milliseconds each time, for a total of 495 milliseconds - just under half a second
            for (byte brightness = MaxBrightness - 1; brightness > 0; brightness -= 3)
            {
                // Set the brightness.
                this.SetBrightnessImmediate(brightness);

                await Task.Delay(15);
            }

            // Return the device to maximum brightness.
            this.SetBrightnessImmediate(MaxBrightness);
        }

        public byte GetBrightnessImmediate()
        {
            return this.QueryDevice(PlasmaTrimCommand.GetBrightness).Skip(1).First();
        }

        public void SetBrightnessImmediate(byte brightness)
        {
            if (brightness > MaxBrightness)
                brightness = MaxBrightness;
            this.QueryDevice(PlasmaTrimCommand.SetBrightness, new byte[] { brightness });
        }

        public IEnumerable<SequenceStep> GetSequence()
        {
            var response = this.QueryDevice(PlasmaTrimCommand.GetSequenceLength);
            var length = response.Skip(1).First();
            var request_step = new byte[30];
            for (byte step = 0; step < length; step++)
            {
                request_step[0] = step;
                var data = this.QueryDevice(PlasmaTrimCommand.GetSequenceStep, request_step);

                // for some reason, the device returns the data in 12 bits, when we need 24...
                var four_bit = data.Skip(2).Take(12);
                var holdfade = data[14];
                var eight_bit = four_bit.SelectMany(b => new[] { b >> 4, b & 0xF }).Select(i => (byte)(i * 16)).ToArray();
                yield return new SequenceStep(GetColorsImpl(eight_bit, 0), (PlasmaTrimTiming)(holdfade >> 4), (PlasmaTrimTiming)(holdfade & 0xF));
            }
        }

        public void SetSequence(IEnumerable<SequenceStep> steps)
        {
            var arr_steps = steps.ToArray();
            if (arr_steps.Length > MaxSequenceSteps)
                throw new ArgumentException("Sequence must have no more than {MaxSequenceSteps} steps", nameof(steps));

            this.SendCommand(PlasmaTrimCommand.SetSequenceLength, new byte[] { (byte)arr_steps.Length  });
            var data = new byte[30];
            for (byte step_index = 0; step_index < arr_steps.Length; step_index++)
            {
                var step = arr_steps[step_index];

                data[0] = step_index; // slot number

                // Fill the buffer with colors!
                var eight_bit = new byte[LedCount * 3];
                var colorIndex = 0;
                foreach (var color in step.Colors)
                {
                    eight_bit[colorIndex++] = color.R;
                    eight_bit[colorIndex++] = color.G;
                    eight_bit[colorIndex++] = color.B;
                }

                var intermediate = eight_bit.Select(b => (b / 16) & 0xF).ToArray();
                colorIndex = 0;
                for (var dataIndex = 0; dataIndex < LedCount * 3 / 2; dataIndex++)
                {
                    data[dataIndex + 1] = (byte)((intermediate[colorIndex++] << 4) + intermediate[colorIndex++]);
                }
                data[13] = (byte)((((byte)step.HoldTime & 0xF) << 4) + ((byte)step.FadeTime & 0xF));
                this.QueryDevice(PlasmaTrimCommand.SetSequenceStep, data);
            }
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

            var name_buffer = this.QueryDevice(PlasmaTrimCommand.GetDeviceName);
            this.Name = Encoding.UTF8.GetString(name_buffer).Trim('\0');
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

        private Color[] GetColorsImpl(byte[] data, byte offset)
        {
            Color[] colors = new Color[LedCount];

            // Build color objects.
            for (var i = 0; i < LedCount; i++)
            {
                // Extract RGB data from this LED position
                var base_index = 3 * i;
                var R = data[base_index + offset];
                var G = data[base_index + offset + 1];
                var B = data[base_index + offset + 2];

                // Create a color.
                colors[i] = Color.FromArgb(R, G, B);
            }
            return colors;
        }

        /// <summary>
        /// Gets an array of one color.
        /// </summary>
        /// <param name="color">The color to project across the array</param>
        /// <returns>A populated array.</returns>
        public static Color[] GetArrayOfColor(Color color)
        {
            return Enumerable.Repeat(color, LedCount).ToArray();
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
