using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasmaTrimAPI
{
    public static class SequenceFile
    {
        public static void WriteSequence(StreamWriter writer, IEnumerable<SequenceStep> sequenceSteps, int activeSlots = -1)
        {
            var steps = sequenceSteps.ToArray();

            writer.WriteLine(@"PlasmaTrim RGB-8 Sequence");
            writer.WriteLine(@"Version: Simple Sequence Format");
            writer.WriteLine(@"Active Slots: {0}", activeSlots == -1 ? steps.Length : activeSlots);

            var index = 0;
            foreach (var step in steps)
            {
                writer.Write(@"slot ");
                writer.Write(index.ToString().PadLeft(2, '0'));
                writer.Write(@" ");
                writer.Write((int)step.HoldTime);
                writer.Write(@" ");
                writer.Write((int)step.FadeTime);
                writer.Write(@" - ");
                foreach (var color in step.Colors)
                {
                    var components = new[] { color.R, color.G, color.B };
                    writer.Write(string.Join(string.Empty, components.Select(b => b.ToString(@"X2")[0])));
                }
                writer.WriteLine();
                index++;
            }
        }

        public static IEnumerable<SequenceStep> ReadSequence(StreamReader reader, out int activeSlots)
        {
            var line = reader.ReadLine();
            if (line != @"PlasmaTrim RGB-8 Sequence")
                throw new InvalidDataException("File is not recognized as a valid PlasmaTrim sequence");
            line = reader.ReadLine();
            if (line != @"Version: Simple Sequence Format")
                throw new InvalidDataException("File is not recognized as a valid PlasmaTrim sequence");

            line = reader.ReadLine();
            if (!line.StartsWith(@"Active Slots: "))
                throw new InvalidDataException("File is not recognized as a valid PlasmaTrim sequence");
            activeSlots = int.Parse(line.Split(new[] { @"Active Slots: " }, StringSplitOptions.RemoveEmptyEntries)[0]);

            return ReadSteps(reader);
        }

        private static IEnumerable<SequenceStep> ReadSteps(StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;
                var items = line.Split(' ');

                var hold = items[2];
                var fade = items[3];
                var colors = new List<Color>();
                for (var index = 0; index < items[5].Length; index += 3)
                {
                    int ToInt(int offset)
                    {
                        return int.Parse(items[5][index + offset].ToString(), System.Globalization.NumberStyles.HexNumber) * 16;
                    }
                    colors.Add(Color.FromArgb(ToInt(0), ToInt(1), ToInt(2)));
                }
                yield return new SequenceStep(colors.ToArray(), (PlasmaTrimTiming)int.Parse(hold), (PlasmaTrimTiming)int.Parse(fade));
            }
        }
    }
}
