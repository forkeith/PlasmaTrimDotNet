using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasmaTrimAPI
{
    public struct SequenceStep
    {
        public Color[] Colors;
        public byte HoldTime;
        public byte FadeTime;

        public SequenceStep (Color[] colors, byte hold = 8, byte fade = 8)
        {
            if (colors.Length != PlasmaTrimController.LedCount)
                throw new ArgumentException($"Color array must contain {PlasmaTrimController.LedCount} elements!", nameof(colors));
            this.Colors = colors;
            this.HoldTime = hold;
            this.FadeTime = fade;
        }
    }
}
