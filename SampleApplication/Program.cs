using PlasmaTrimAPI;
using System.Drawing;

// First, let's enumerate all the PlasmaTrim devices connected.
var devices = PlasmaTrimEnumerator.FindConnected().ToArray();

// List out all the devices we've located.
Console.WriteLine("Located {0} PlasmaTrim devices:", devices.Length);

// Iterate over 'em and display 'em.
for (var i = 0; i < devices.Length; i++)
{
    Console.WriteLine("[{0}] SN#{1}", i, devices[i].SerialNumber);
}

// Pause, wait for input.
Console.WriteLine("Press any key to start!");
Console.ReadKey();

// We're going to operate on all attached devices.
foreach (var device in devices)
{
    try
    {
        // Open a connection to this device.
        Console.WriteLine("[{0}] Opening connection to {0}.", device.SerialNumber);
        device.OpenDevice();

        // Start off by stopping the animation.
        Console.WriteLine("[{0}] Stopping animation.", device.SerialNumber);
        device.StopStoredSequence();
        
        // Wait for it...
        Console.WriteLine("[{0}] Pausing 2 seconds.", device.SerialNumber);
        Thread.Sleep(2000);
        
        // Restart the animation.
        Console.WriteLine("[{0}] Starting animation.", device.SerialNumber);
        device.PlayStoredSequence();

        // Wait for it...
        Console.WriteLine("[{0}] Pausing 2 seconds.", device.SerialNumber);
        Thread.Sleep(2000);

        // Iterate over a few colors for the sake of testing.
        Color[] testColors = new Color[] { Color.Red, Color.Blue, Color.Green, Color.White, Color.Pink, Color.Yellow, Color.Aquamarine, Color.Cyan };

        foreach (var color in testColors)
        {
            Console.WriteLine("[{0}] Color Test.", device.SerialNumber);
            device.SetColorsImmediate(PlasmaTrimController.GetArrayOfColor(color), PlasmaTrimController.MaxBrightness);

            var requestedColors = device.GetColorsImmediate();

            Console.WriteLine("[{0}] Color at position 1: {1}, {2}, {3}", device.SerialNumber, requestedColors[0].R, requestedColors[0].G, requestedColors[0].B);
            Thread.Sleep(750);
        }

        // Test pulse
        foreach (var color in testColors)
        {
            Console.WriteLine("[{0}] Pulse Test {1}, {2}, {3}.", device.SerialNumber, color.R, color.G, color.B);
            device.PulseColor(color).Wait();
        }
        
        var seq = device.GetSequence(false).ToArray();
        // write the sequence to a file
        using (var sw = File.CreateText(@"./sequence.ptSeq"))
        {
            SequenceFile.WriteSequence(sw, seq);
        }
        // prove we can read it back again
        using (var sr = new StreamReader(@"./sequence.ptSeq"))
        {
            var seq2 = SequenceFile.ReadSequence(sr, out var active).Take(active).ToArray();
        }
        // update the sequence on the device (this currently just sets it to the same sequence)
        //device.SetSequence(seq);

        // Restart the animation.
        Console.WriteLine("[{0}] Starting animation.", device.SerialNumber);
        device.PlayStoredSequence();
    }
    finally
    {
        // Close a connection to this device.
        Console.WriteLine("[{0}] Closing connection to {0}.", device.SerialNumber);
        device.CloseDevice();
    }
}

// Pause, wait for input.
if (devices.Any())
{
    Console.WriteLine("Finished - press any key to exit.");
    Console.ReadKey();
}
