using GNSS卫星坐标计算.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GNSS卫星坐标计算.Core
{
    public class RinexNavReader
    {
        public List<GnssEphemeris> ReadAnyNavFile(string filePath)
        {
            List<GnssEphemeris> ephList = new List<GnssEphemeris>();
            try
            {
                string[] allLines = ReadRinexLines(filePath);
                if (allLines.Length == 0) return ephList;

                RinexHeaderInfo header = ReadHeader(allLines, filePath);
                if (!header.HasHeader)
                    throw new InvalidDataException("未识别到RINEX文件头：缺少 RINEX VERSION / TYPE。");
                if (!header.IsNavigation)
                    throw new InvalidDataException("当前文件不是导航星历文件。请打开 NAV/RNX 导航文件，不要选择 OBS/O 观测文件。");

                int curLine = header.DataStartLine;
                while (curLine < allLines.Length)
                {
                    if (string.IsNullOrWhiteSpace(allLines[curLine]))
                    {
                        curLine++;
                        continue;
                    }

                    FirstLineInfo first = ParseFirstLine(allLines[curLine], header.DefaultSystem);
                    if (!first.Valid)
                    {
                        curLine++;
                        continue;
                    }

                    if (IsStateVectorSystem(first.System))
                    {
                        if (curLine + 3 >= allLines.Length) break;
                        GnssEphemeris eph = ParseStateVectorRecord(allLines, curLine, first, header.Version);
                        if (eph != null) ephList.Add(eph);
                        curLine += 4;
                    }
                    else
                    {
                        if (curLine + 7 >= allLines.Length) break;
                        GnssEphemeris eph = ParseKeplerRecord(allLines, curLine, first, header.Version);
                        if (eph != null) ephList.Add(eph);
                        curLine += 8;
                    }
                }
            }
            catch
            {
                throw;
            }
            return ephList;
        }

        public List<GpsEphemeris> ReadNavFile(string filePath)
        {
            List<GpsEphemeris> gpsList = new List<GpsEphemeris>();
            foreach (GnssEphemeris eph in ReadAnyNavFile(filePath))
            {
                if (eph.Kind != EphemerisKind.BroadcastKepler) continue;
                gpsList.Add(ToGpsEphemeris(eph));
            }
            return gpsList;
        }

        private string[] ReadRinexLines(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".gz")
            {
                using (FileStream fs = File.OpenRead(filePath))
                using (GZipStream gz = new GZipStream(fs, CompressionMode.Decompress))
                using (StreamReader sr = new StreamReader(gz, Encoding.ASCII, true))
                    return SplitLines(sr.ReadToEnd());
            }

            if (ext == ".zip")
            {
                using (ZipArchive zip = ZipFile.OpenRead(filePath))
                {
                    ZipArchiveEntry entry = zip.Entries
                        .Where(e => !string.IsNullOrEmpty(e.Name))
                        .OrderByDescending(e => LooksLikeNavigationName(e.Name))
                        .ThenBy(e => e.FullName)
                        .FirstOrDefault();
                    if (entry == null)
                        throw new InvalidDataException("ZIP压缩包中没有可读取的RINEX文件。");
                    using (Stream stream = entry.Open())
                    using (StreamReader sr = new StreamReader(stream, Encoding.ASCII, true))
                        return SplitLines(sr.ReadToEnd());
                }
            }

            return File.ReadAllLines(filePath, Encoding.Default);
        }

        private string[] SplitLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private bool LooksLikeNavigationName(string name)
        {
            string lower = name.ToLowerInvariant();
            if (lower.EndsWith(".nav") || lower.EndsWith(".rnx")) return true;
            if (Regex.IsMatch(lower, @"\.\d{2}[nglp]$")) return true;
            if (Regex.IsMatch(lower, @"\.[nglp]$")) return true;
            return false;
        }

        private RinexHeaderInfo ReadHeader(string[] lines, string filePath)
        {
            RinexHeaderInfo info = new RinexHeaderInfo();
            info.Version = 2.0;
            info.DefaultSystem = SystemFromExtension(filePath);
            info.DataStartLine = 0;
            info.HasHeader = false;
            info.IsNavigation = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains("RINEX VERSION / TYPE"))
                {
                    info.HasHeader = true;
                    double version;
                    if (double.TryParse(line.Substring(0, Math.Min(9, line.Length)).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out version))
                        info.Version = version;

                    string upper = line.ToUpperInvariant();
                    info.IsNavigation = upper.Contains("NAV") || upper.Contains("NAVIGATION") || Regex.IsMatch(upper, @"\bN\b");
                    if (upper.Contains("OBSERVATION") || Regex.IsMatch(upper, @"\bOBS\b") || Regex.IsMatch(upper, @"\bO\b\s+OBSERVATION"))
                        info.IsNavigation = false;

                    if (upper.Contains("GLONASS")) info.DefaultSystem = GnssSystem.GLONASS;
                    else if (upper.Contains("GALILEO")) info.DefaultSystem = GnssSystem.Galileo;
                    else if (upper.Contains("BEIDOU") || upper.Contains("BDS")) info.DefaultSystem = GnssSystem.BeiDou;
                    else if (upper.Contains("QZSS")) info.DefaultSystem = GnssSystem.QZSS;
                    else if (upper.Contains("IRNSS") || upper.Contains("NAVIC")) info.DefaultSystem = GnssSystem.IRNSS;
                    else if (upper.Contains("SBAS")) info.DefaultSystem = GnssSystem.SBAS;
                    else if (upper.Contains("MIXED") || upper.Contains("GNSS")) info.DefaultSystem = GnssSystem.Unknown;
                    else if (upper.Contains("GPS")) info.DefaultSystem = GnssSystem.GPS;
                }
                if (line.Contains("END OF HEADER"))
                {
                    info.DataStartLine = i + 1;
                    break;
                }
            }
            return info;
        }

        private FirstLineInfo ParseFirstLine(string line, GnssSystem defaultSystem)
        {
            FirstLineInfo info = new FirstLineInfo();
            info.Valid = false;
            string text = line.TrimStart();
            if (text.Length == 0) return info;

            char firstChar = text[0];
            if (char.IsLetter(firstChar))
            {
                Match m = Regex.Match(text, @"^([A-Za-z])\s*(\d{1,3})");
                if (!m.Success) return info;
                info.SystemCode = m.Groups[1].Value.ToUpperInvariant();
                info.System = SystemFromCode(info.SystemCode);
                int.TryParse(m.Groups[2].Value, out info.PRN);
            }
            else
            {
                List<double> nums = ReadNums(line);
                if (nums.Count < 7) return info;
                info.System = defaultSystem == GnssSystem.Unknown ? GnssSystem.GPS : defaultSystem;
                info.SystemCode = CodeFromSystem(info.System);
                info.PRN = (int)nums[0];
            }

            List<double> allNums = ReadNums(line);
            int offset = 1;
            if (allNums.Count < offset + 6) return info;
            int year = ConvertYear((int)allNums[offset]);
            int month = (int)allNums[offset + 1];
            int day = (int)allNums[offset + 2];
            int hour = (int)allNums[offset + 3];
            int minute = (int)allNums[offset + 4];
            double second = allNums[offset + 5];
            info.Epoch = MakeDateTime(year, month, day, hour, minute, second);
            info.Clock0 = GetOrNaN(allNums, offset + 6);
            info.Clock1 = GetOrNaN(allNums, offset + 7);
            info.Clock2 = GetOrNaN(allNums, offset + 8);
            info.Valid = info.PRN > 0 && info.Epoch != DateTime.MinValue && info.System != GnssSystem.Unknown;
            return info;
        }

        private GnssEphemeris ParseKeplerRecord(string[] lines, int start, FirstLineInfo first, double version)
        {
            List<double> l2 = ReadNums(lines[start + 1]);
            List<double> l3 = ReadNums(lines[start + 2]);
            List<double> l4 = ReadNums(lines[start + 3]);
            List<double> l5 = ReadNums(lines[start + 4]);
            List<double> l6 = ReadNums(lines[start + 5]);
            if (l2.Count < 4 || l3.Count < 4 || l4.Count < 4 || l5.Count < 4 || l6.Count < 1)
                return null;

            GnssEphemeris eph = NewBase(first, EphemerisKind.BroadcastKepler, version);
            eph.IODE = l2[0];
            eph.Crs = l2[1];
            eph.DeltaN = l2[2];
            eph.M0 = l2[3];
            eph.Cuc = l3[0];
            eph.Ecc = l3[1];
            eph.Cus = l3[2];
            eph.SqrtA = l3[3];
            eph.Toe = l4[0];
            eph.Cic = l4[1];
            eph.Omega0 = l4[2];
            eph.Cis = l4[3];
            eph.I0 = l5[0];
            eph.Crc = l5[1];
            eph.Omega = l5[2];
            eph.OmegaDot = l5[3];
            eph.Idot = l6[0];
            if (l6.Count > 2) eph.Week = (int)Math.Round(l6[2]);
            return double.IsNaN(eph.SqrtA) || eph.SqrtA == 0 ? null : eph;
        }

        private GnssEphemeris ParseStateVectorRecord(string[] lines, int start, FirstLineInfo first, double version)
        {
            List<double> l2 = ReadNums(lines[start + 1]);
            List<double> l3 = ReadNums(lines[start + 2]);
            List<double> l4 = ReadNums(lines[start + 3]);
            if (l2.Count < 4 || l3.Count < 4 || l4.Count < 4) return null;

            GnssEphemeris eph = NewBase(first, EphemerisKind.GlonassStateVector, version);
            eph.X = l2[0] * 1000.0;
            eph.XDot = l2[1] * 1000.0;
            eph.XAcc = l2[2] * 1000.0;
            eph.Health = l2[3];
            eph.Y = l3[0] * 1000.0;
            eph.YDot = l3[1] * 1000.0;
            eph.YAcc = l3[2] * 1000.0;
            eph.FrequencyNumber = l3[3];
            eph.Z = l4[0] * 1000.0;
            eph.ZDot = l4[1] * 1000.0;
            eph.ZAcc = l4[2] * 1000.0;
            eph.AgeOfOperation = l4[3];
            return eph;
        }

        private bool IsStateVectorSystem(GnssSystem system)
        {
            return system == GnssSystem.GLONASS || system == GnssSystem.SBAS;
        }

        private GnssEphemeris NewBase(FirstLineInfo first, EphemerisKind kind, double version)
        {
            return new GnssEphemeris
            {
                System = first.System,
                SystemCode = first.SystemCode,
                PRN = first.PRN,
                Kind = kind,
                EpochTime = first.Epoch,
                ClockBias = first.Clock0,
                ClockDrift = first.Clock1,
                ClockDriftRate = first.Clock2,
                SourceFormat = "RINEX " + version.ToString("0.00", CultureInfo.InvariantCulture)
            };
        }

        private GpsEphemeris ToGpsEphemeris(GnssEphemeris src)
        {
            return new GpsEphemeris
            {
                PRN = src.PRN,
                EpocTime = src.EpochTime,
                Af0 = src.ClockBias,
                Af1 = src.ClockDrift,
                Af2 = src.ClockDriftRate,
                IODE = src.IODE,
                Crs = src.Crs,
                DeltaN = src.DeltaN,
                M0 = src.M0,
                Cuc = src.Cuc,
                Ecc = src.Ecc,
                Cus = src.Cus,
                SqrtA = src.SqrtA,
                Toe = src.Toe,
                Cic = src.Cic,
                Omega0 = src.Omega0,
                Cis = src.Cis,
                I0 = src.I0,
                Crc = src.Crc,
                Omega = src.Omega,
                OmegaDot = src.OmegaDot,
                Idot = src.Idot,
                Week = src.Week
            };
        }

        private List<double> ReadNums(string content)
        {
            List<double> nums = new List<double>();
            if (string.IsNullOrWhiteSpace(content)) return nums;
            Regex numReg = new Regex(@"[-+]?\d+(?:\.\d*)?(?:[DEe][+-]?\d+)?|[-+]?\.\d+(?:[DEe][+-]?\d+)?");
            foreach (Match match in numReg.Matches(content))
            {
                string numStr = match.Value.ToUpperInvariant().Replace("D", "E");
                double value;
                if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    nums.Add(value);
            }
            return nums;
        }

        private double GetOrNaN(List<double> nums, int index)
        {
            return index >= 0 && index < nums.Count ? nums[index] : double.NaN;
        }

        private int ConvertYear(int year)
        {
            if (year >= 100) return year;
            return year >= 80 ? 1900 + year : 2000 + year;
        }

        private DateTime MakeDateTime(int year, int month, int day, int hour, int minute, double second)
        {
            try
            {
                int wholeSecond = (int)Math.Floor(second);
                int millis = (int)Math.Round((second - wholeSecond) * 1000.0);
                return new DateTime(year, month, day, hour, minute, wholeSecond, DateTimeKind.Utc).AddMilliseconds(millis);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private GnssSystem SystemFromExtension(string filePath)
        {
            string name = Path.GetFileName(filePath).ToLowerInvariant();
            if (Regex.IsMatch(name, @"\.\d{2}g$") || name.EndsWith(".g")) return GnssSystem.GLONASS;
            if (Regex.IsMatch(name, @"\.\d{2}n$") || name.EndsWith(".n")) return GnssSystem.GPS;
            if (Regex.IsMatch(name, @"\.\d{2}l$") || name.EndsWith(".l")) return GnssSystem.Galileo;
            if (Regex.IsMatch(name, @"\.\d{2}p$") || name.EndsWith(".p")) return GnssSystem.Unknown;
            return GnssSystem.GPS;
        }

        private GnssSystem SystemFromCode(string code)
        {
            switch ((code ?? "").ToUpperInvariant())
            {
                case "G": return GnssSystem.GPS;
                case "R": return GnssSystem.GLONASS;
                case "E": return GnssSystem.Galileo;
                case "C": return GnssSystem.BeiDou;
                case "J": return GnssSystem.QZSS;
                case "S": return GnssSystem.SBAS;
                case "I": return GnssSystem.IRNSS;
                default: return GnssSystem.Unknown;
            }
        }

        private string CodeFromSystem(GnssSystem system)
        {
            switch (system)
            {
                case GnssSystem.GPS: return "G";
                case GnssSystem.GLONASS: return "R";
                case GnssSystem.Galileo: return "E";
                case GnssSystem.BeiDou: return "C";
                case GnssSystem.QZSS: return "J";
                case GnssSystem.SBAS: return "S";
                case GnssSystem.IRNSS: return "I";
                default: return "?";
            }
        }

        private class RinexHeaderInfo
        {
            public bool HasHeader;
            public bool IsNavigation;
            public double Version;
            public GnssSystem DefaultSystem;
            public int DataStartLine;
        }

        private class FirstLineInfo
        {
            public bool Valid;
            public GnssSystem System;
            public string SystemCode;
            public int PRN;
            public DateTime Epoch;
            public double Clock0;
            public double Clock1;
            public double Clock2;
        }
    }
}
