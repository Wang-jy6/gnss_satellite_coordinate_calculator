using System;
using GNSS卫星坐标计算.Models;

namespace GNSS卫星坐标计算.Core
{
    /// <summary>
    /// 多系统广播星历卫星ECEF坐标计算。
    /// GPS/Galileo/BDS/QZSS/SBAS/IRNSS使用广播开普勒参数；
    /// GLONASS使用星历历元状态向量按速度/加速度二阶外推。
    /// </summary>
    public class SatOrbitCalculator
    {
        public SatPositionResult CalcSatPosition(GnssEphemeris eph, double tk)
        {
            if (eph == null) throw new ArgumentNullException("eph");

            if (eph.Kind == EphemerisKind.GlonassStateVector)
                return CalcGlonassStateVector(eph, tk);

            if (eph.Kind == EphemerisKind.BroadcastKepler)
                return CalcBroadcastKepler(eph, tk);

            throw new NotSupportedException("未知星历类型，无法计算坐标。");
        }

        public SatPositionResult CalcSatPosition(GpsEphemeris eph, double tk)
        {
            GnssEphemeris unified = new GnssEphemeris
            {
                System = GnssSystem.GPS,
                SystemCode = "G",
                PRN = eph.PRN,
                Kind = EphemerisKind.BroadcastKepler,
                EpochTime = eph.EpocTime,
                ClockBias = eph.Af0,
                ClockDrift = eph.Af1,
                ClockDriftRate = eph.Af2,
                IODE = eph.IODE,
                Crs = eph.Crs,
                DeltaN = eph.DeltaN,
                M0 = eph.M0,
                Cuc = eph.Cuc,
                Ecc = eph.Ecc,
                Cus = eph.Cus,
                SqrtA = eph.SqrtA,
                Toe = eph.Toe,
                Cic = eph.Cic,
                Omega0 = eph.Omega0,
                Cis = eph.Cis,
                I0 = eph.I0,
                Crc = eph.Crc,
                Omega = eph.Omega,
                OmegaDot = eph.OmegaDot,
                Idot = eph.Idot,
                Week = eph.Week
            };
            return CalcSatPosition(unified, tk);
        }

        private SatPositionResult CalcBroadcastKepler(GnssEphemeris eph, double tk)
        {
            double mu;
            double omegaE;
            GetConstants(eph.System, out mu, out omegaE);

            double A = eph.SqrtA * eph.SqrtA;
            double n0 = Math.Sqrt(mu / (A * A * A));
            double n = n0 + eph.DeltaN;

            double M = eph.M0 + n * tk;
            double E = M;
            for (int iter = 0; iter < 30; iter++)
            {
                double dE = (M - (E - eph.Ecc * Math.Sin(E))) / (1 - eph.Ecc * Math.Cos(E));
                E += dE;
                if (Math.Abs(dE) < 1e-14) break;
            }

            double nu = Math.Atan2(
                Math.Sqrt(1 - eph.Ecc * eph.Ecc) * Math.Sin(E),
                Math.Cos(E) - eph.Ecc);

            double phi = nu + eph.Omega;
            double du = eph.Cuc * Math.Cos(2 * phi) + eph.Cus * Math.Sin(2 * phi);
            double dr = eph.Crc * Math.Cos(2 * phi) + eph.Crs * Math.Sin(2 * phi);
            double di = eph.Cic * Math.Cos(2 * phi) + eph.Cis * Math.Sin(2 * phi);

            double u = phi + du;
            double r = A * (1 - eph.Ecc * Math.Cos(E)) + dr;
            double iAngle = eph.I0 + di + eph.Idot * tk;
            double omega = eph.Omega0 + (eph.OmegaDot - omegaE) * tk - omegaE * eph.Toe;

            double xOrb = r * Math.Cos(u);
            double yOrb = r * Math.Sin(u);

            double x = xOrb * Math.Cos(omega) - yOrb * Math.Cos(iAngle) * Math.Sin(omega);
            double y = xOrb * Math.Sin(omega) + yOrb * Math.Cos(iAngle) * Math.Cos(omega);
            double z = yOrb * Math.Sin(iAngle);

            return BuildResult(eph, tk, x, y, z, "BroadcastKepler");
        }

        private SatPositionResult CalcGlonassStateVector(GnssEphemeris eph, double tk)
        {
            double x = eph.X + eph.XDot * tk + 0.5 * eph.XAcc * tk * tk;
            double y = eph.Y + eph.YDot * tk + 0.5 * eph.YAcc * tk * tk;
            double z = eph.Z + eph.ZDot * tk + 0.5 * eph.ZAcc * tk * tk;
            return BuildResult(eph, tk, x, y, z, "GlonassStateVectorSecondOrder");
        }

        private SatPositionResult BuildResult(GnssEphemeris eph, double tk, double x, double y, double z, string method)
        {
            return new SatPositionResult
            {
                SystemCode = eph.SystemCode,
                PRN = eph.PRN,
                SatelliteId = eph.SatelliteId,
                CalcTime = eph.EpochTime == DateTime.MinValue ? DateTime.Now : eph.EpochTime.AddSeconds(tk),
                X = x,
                Y = y,
                Z = z,
                Method = method
            };
        }

        private void GetConstants(GnssSystem system, out double mu, out double omegaE)
        {
            if (system == GnssSystem.BeiDou)
            {
                mu = GpsConst.MU_CGCS2000;
                omegaE = GpsConst.OmegaE_CGCS2000;
            }
            else if (system == GnssSystem.Galileo)
            {
                mu = GpsConst.MU_Galileo;
                omegaE = GpsConst.OmegaE_Galileo;
            }
            else
            {
                mu = GpsConst.MU;
                omegaE = GpsConst.OmegaE;
            }
        }
    }
}
