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
        public static void WriteSequence(StreamWriter writer, IEnumerable<SequenceStep> sequenceSteps)
        {
            var steps = sequenceSteps.ToArray();

            writer.WriteLine(@"PlasmaTrim RGB-8 Sequence");
            writer.WriteLine(@"Version: Simple Sequence Format");
            writer.WriteLine(@"Active Slots: " + steps.Length);

            var index = 0;
            foreach (var step in sequenceSteps)
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
    }
}
