using System;

namespace GNSS卫星坐标计算.Models
{
    /// <summary>
    /// 卫星ECEF地心地固坐标计算结果
    /// </summary>
    public class SatPositionResult
    {
        public string SystemCode { get; set; }
        public int PRN { get; set; }
        public string SatelliteId { get; set; }
        public DateTime CalcTime { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string Method { get; set; }
    }
}
