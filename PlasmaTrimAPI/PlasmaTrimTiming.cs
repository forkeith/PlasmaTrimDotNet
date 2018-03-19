using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasmaTrimAPI
{
    public enum PlasmaTrimTiming : byte
    {
        Instantly = 0,
        OneTenthOfASecond = 1,
        OneQuarterOfASecond = 2,
        HalfASecond = 3,
        OneSecond = 4,
        TwoAndAHalfSeconds = 5,
        FiveSeconds = 6,
        TenSeconds = 7,
        FifteenSeconds = 8,
        ThirtySeconds = 9,
        OneMinute = 10,
        TwoAndAHalfMinutes = 11,
        FiveMinutes = 12,
        TenMinutes = 13,
        FifteenMinutes = 14,
        ThirtyMinutes = 15,
    }
}
