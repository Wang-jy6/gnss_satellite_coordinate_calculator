using System;

namespace GNSS卫星坐标计算.Models
{
    /// <summary>
    /// GPS广播星历 RINEX导航文件参数
    /// </summary>
    public class GpsEphemeris
    {
        public int PRN { get; set; }
        public DateTime EpocTime { get; set; }
        public double Toc { get; set; }
        public double Af0 { get; set; }
        public double Af1 { get; set; }
        public double Af2 { get; set; }

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
    }
}