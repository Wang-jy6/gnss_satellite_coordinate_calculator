using System;
using System.Collections.Generic;
using System.Data.SQLite;
using GNSS卫星坐标计算.Models;

namespace GNSS卫星坐标计算.Data
{
    public static class SqliteHelper
    {
        private static readonly string dbFile = "gpsdata.db";
        public static string ConnStr { get { return "Data Source=" + dbFile + ";Version=3;"; } }

        public static void InitDB()
        {
            using (SQLiteConnection conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                string sqlEph = @"CREATE TABLE IF NOT EXISTS Ephemeris(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SystemCode TEXT,
                    PRN INT,
                    SatelliteId TEXT,
                    Kind TEXT,
                    EpochTime TEXT,
                    Toe REAL,
                    SqrtA REAL,
                    Ecc REAL,
                    M0 REAL,
                    Omega REAL,
                    Omega0 REAL,
                    I0 REAL,
                    DeltaN REAL,
                    OmegaDot REAL,
                    Idot REAL,
                    X REAL,
                    Y REAL,
                    Z REAL,
                    XDot REAL,
                    YDot REAL,
                    ZDot REAL
                )";
                new SQLiteCommand(sqlEph, conn).ExecuteNonQuery();

                EnsureColumn(conn, "Ephemeris", "SystemCode", "TEXT");
                EnsureColumn(conn, "Ephemeris", "SatelliteId", "TEXT");
                EnsureColumn(conn, "Ephemeris", "Kind", "TEXT");
                EnsureColumn(conn, "Ephemeris", "EpochTime", "TEXT");
                EnsureColumn(conn, "Ephemeris", "X", "REAL");
                EnsureColumn(conn, "Ephemeris", "Y", "REAL");
                EnsureColumn(conn, "Ephemeris", "Z", "REAL");
                EnsureColumn(conn, "Ephemeris", "XDot", "REAL");
                EnsureColumn(conn, "Ephemeris", "YDot", "REAL");
                EnsureColumn(conn, "Ephemeris", "ZDot", "REAL");

                string sqlPos = @"CREATE TABLE IF NOT EXISTS SatPosition(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SystemCode TEXT,
                    PRN INT,
                    SatelliteId TEXT,
                    CalcTime TEXT,
                    X REAL,
                    Y REAL,
                    Z REAL,
                    Method TEXT
                )";
                new SQLiteCommand(sqlPos, conn).ExecuteNonQuery();

                EnsureColumn(conn, "SatPosition", "SystemCode", "TEXT");
                EnsureColumn(conn, "SatPosition", "SatelliteId", "TEXT");
                EnsureColumn(conn, "SatPosition", "Method", "TEXT");
            }
        }

        public static void SaveEphemeris(GnssEphemeris eph)
        {
            SaveEphemerisBatch(new List<GnssEphemeris> { eph });
        }

        public static void SaveEphemeris(GpsEphemeris eph)
        {
            SaveEphemeris(new GnssEphemeris
            {
                System = GnssSystem.GPS,
                SystemCode = "G",
                PRN = eph.PRN,
                Kind = EphemerisKind.BroadcastKepler,
                EpochTime = eph.EpocTime,
                Toe = eph.Toe,
                SqrtA = eph.SqrtA,
                Ecc = eph.Ecc,
                M0 = eph.M0,
                Omega = eph.Omega,
                Omega0 = eph.Omega0,
                I0 = eph.I0,
                DeltaN = eph.DeltaN,
                OmegaDot = eph.OmegaDot,
                Idot = eph.Idot
            });
        }

        public static void SaveEphemerisBatch(IEnumerable<GnssEphemeris> ephList)
        {
            using (SQLiteConnection conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (SQLiteTransaction tran = conn.BeginTransaction())
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"INSERT INTO Ephemeris
                        (SystemCode,PRN,SatelliteId,Kind,EpochTime,Toe,SqrtA,Ecc,M0,Omega,Omega0,I0,DeltaN,OmegaDot,Idot,X,Y,Z,XDot,YDot,ZDot)
                        VALUES(@SystemCode,@PRN,@SatelliteId,@Kind,@EpochTime,@Toe,@SqrtA,@Ecc,@M0,@Omega,@Omega0,@I0,@DeltaN,@OmegaDot,@Idot,@X,@Y,@Z,@XDot,@YDot,@ZDot)";
                    AddEphemerisParameters(cmd);

                    foreach (var eph in ephList)
                    {
                        FillEphemerisParameters(cmd, eph);
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();
                }
            }
        }

        public static void SaveResult(SatPositionResult res)
        {
            SaveResultBatch(new List<SatPositionResult> { res });
        }

        public static void SaveResultBatch(IEnumerable<SatPositionResult> results)
        {
            using (SQLiteConnection conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (SQLiteTransaction tran = conn.BeginTransaction())
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"INSERT INTO SatPosition(SystemCode,PRN,SatelliteId,CalcTime,X,Y,Z,Method)
                        VALUES(@SystemCode,@PRN,@SatelliteId,@t,@X,@Y,@Z,@Method)";
                    cmd.Parameters.Add("@SystemCode", System.Data.DbType.String);
                    cmd.Parameters.Add("@PRN", System.Data.DbType.Int32);
                    cmd.Parameters.Add("@SatelliteId", System.Data.DbType.String);
                    cmd.Parameters.Add("@t", System.Data.DbType.String);
                    cmd.Parameters.Add("@X", System.Data.DbType.Double);
                    cmd.Parameters.Add("@Y", System.Data.DbType.Double);
                    cmd.Parameters.Add("@Z", System.Data.DbType.Double);
                    cmd.Parameters.Add("@Method", System.Data.DbType.String);

                    foreach (var res in results)
                    {
                        cmd.Parameters["@SystemCode"].Value = res.SystemCode ?? "";
                        cmd.Parameters["@PRN"].Value = res.PRN;
                        cmd.Parameters["@SatelliteId"].Value = res.SatelliteId ?? "";
                        cmd.Parameters["@t"].Value = res.CalcTime.ToString("yyyy-MM-dd HH:mm:ss");
                        cmd.Parameters["@X"].Value = res.X;
                        cmd.Parameters["@Y"].Value = res.Y;
                        cmd.Parameters["@Z"].Value = res.Z;
                        cmd.Parameters["@Method"].Value = res.Method ?? "";
                        cmd.ExecuteNonQuery();
                    }
                    tran.Commit();
                }
            }
        }

        private static void AddEphemerisParameters(SQLiteCommand cmd)
        {
            cmd.Parameters.Add("@SystemCode", System.Data.DbType.String);
            cmd.Parameters.Add("@PRN", System.Data.DbType.Int32);
            cmd.Parameters.Add("@SatelliteId", System.Data.DbType.String);
            cmd.Parameters.Add("@Kind", System.Data.DbType.String);
            cmd.Parameters.Add("@EpochTime", System.Data.DbType.String);
            cmd.Parameters.Add("@Toe", System.Data.DbType.Double);
            cmd.Parameters.Add("@SqrtA", System.Data.DbType.Double);
            cmd.Parameters.Add("@Ecc", System.Data.DbType.Double);
            cmd.Parameters.Add("@M0", System.Data.DbType.Double);
            cmd.Parameters.Add("@Omega", System.Data.DbType.Double);
            cmd.Parameters.Add("@Omega0", System.Data.DbType.Double);
            cmd.Parameters.Add("@I0", System.Data.DbType.Double);
            cmd.Parameters.Add("@DeltaN", System.Data.DbType.Double);
            cmd.Parameters.Add("@OmegaDot", System.Data.DbType.Double);
            cmd.Parameters.Add("@Idot", System.Data.DbType.Double);
            cmd.Parameters.Add("@X", System.Data.DbType.Double);
            cmd.Parameters.Add("@Y", System.Data.DbType.Double);
            cmd.Parameters.Add("@Z", System.Data.DbType.Double);
            cmd.Parameters.Add("@XDot", System.Data.DbType.Double);
            cmd.Parameters.Add("@YDot", System.Data.DbType.Double);
            cmd.Parameters.Add("@ZDot", System.Data.DbType.Double);
        }

        private static void FillEphemerisParameters(SQLiteCommand cmd, GnssEphemeris eph)
        {
            cmd.Parameters["@SystemCode"].Value = eph.SystemCode ?? "";
            cmd.Parameters["@PRN"].Value = eph.PRN;
            cmd.Parameters["@SatelliteId"].Value = eph.SatelliteId ?? "";
            cmd.Parameters["@Kind"].Value = eph.Kind.ToString();
            cmd.Parameters["@EpochTime"].Value = eph.EpochTime == DateTime.MinValue ? "" : eph.EpochTime.ToString("yyyy-MM-dd HH:mm:ss");
            cmd.Parameters["@Toe"].Value = eph.Toe;
            cmd.Parameters["@SqrtA"].Value = eph.SqrtA;
            cmd.Parameters["@Ecc"].Value = eph.Ecc;
            cmd.Parameters["@M0"].Value = eph.M0;
            cmd.Parameters["@Omega"].Value = eph.Omega;
            cmd.Parameters["@Omega0"].Value = eph.Omega0;
            cmd.Parameters["@I0"].Value = eph.I0;
            cmd.Parameters["@DeltaN"].Value = eph.DeltaN;
            cmd.Parameters["@OmegaDot"].Value = eph.OmegaDot;
            cmd.Parameters["@Idot"].Value = eph.Idot;
            cmd.Parameters["@X"].Value = eph.X;
            cmd.Parameters["@Y"].Value = eph.Y;
            cmd.Parameters["@Z"].Value = eph.Z;
            cmd.Parameters["@XDot"].Value = eph.XDot;
            cmd.Parameters["@YDot"].Value = eph.YDot;
            cmd.Parameters["@ZDot"].Value = eph.ZDot;
        }

        private static void EnsureColumn(SQLiteConnection conn, string table, string column, string type)
        {
            try
            {
                new SQLiteCommand("ALTER TABLE " + table + " ADD COLUMN " + column + " " + type, conn).ExecuteNonQuery();
            }
            catch
            {
                // Existing column, ignore.
            }
        }
    }
}
