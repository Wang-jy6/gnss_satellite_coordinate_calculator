using System;

namespace GNSS卫星坐标计算.Models
{
    public static class GpsConst
    {
        public const double MU = 3.986005e14;         //地心引力常数
        public const double OmegaE = 7.2921151467e-5; //地球自转角速度rad/s
        public const double PI = Math.PI;

        public const double MU_CGCS2000 = 3.986004418e14;
        public const double OmegaE_CGCS2000 = 7.2921150e-5;
        public const double MU_Galileo = 3.986004418e14;
        public const double OmegaE_Galileo = 7.2921151467e-5;
    }
}
