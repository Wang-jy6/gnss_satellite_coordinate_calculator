using System;

namespace GNSS卫星坐标计算.Models
{
    public enum GnssSystem
    {
        Unknown,
        GPS,
        GLONASS,
        Galileo,
        BeiDou,
        QZSS,
        SBAS,
        IRNSS
    }

    public enum EphemerisKind
    {
        BroadcastKepler,
        GlonassStateVector,
        Unknown
    }

    public class GnssEphemeris
    {
        public GnssSystem System { get; set; }
        public string SystemCode { get; set; }
        public int PRN { get; set; }
        public string SatelliteId
        {
            get { return (SystemCode ?? "?") + PRN.ToString("00"); }
        }

        public EphemerisKind Kind { get; set; }
        public DateTime EpochTime { get; set; }

        public double ClockBias { get; set; }
        public double ClockDrift { get; set; }
        public double ClockDriftRate { get; set; }

        public double IODE { get; set; }
        public double Crs { get; set; }
        public double DeltaN { get; set; }
        public double M0 { get; set; }
        public double Cuc { get; set; }
        public double Ecc { get; set; }
        public double Cus { get; set; }
        public double SqrtA { get; set; }
        public double Toe { get; set; }
        public double Cic { get; set; }
        public double Omega0 { get; set; }
        public double Cis { get; set; }
        public double I0 { get; set; }
        public double Crc { get; set; }
        public double Omega { get; set; }
        public double OmegaDot { get; set; }
        public double Idot { get; set; }
        public int Week { get; set; }

        public double X { get; set; }
        public double XDot { get; set; }
        public double XAcc { get; set; }
        public double Y { get; set; }
        public double YDot { get; set; }
        public double YAcc { get; set; }
        public double Z { get; set; }
        public double ZDot { get; set; }
        public double ZAcc { get; set; }
        public double Health { get; set; }
        public double FrequencyNumber { get; set; }
        public double AgeOfOperation { get; set; }

        public string SourceFormat { get; set; }
    }
}
