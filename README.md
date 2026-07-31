# GNSS Satellite Coordinate Calculator

GNSS Satellite Coordinate Calculator is a C# WinForms application for reading
RINEX navigation ephemeris files and calculating GNSS satellite coordinates in
the ECEF coordinate system.

## Features

- Read RINEX navigation files.
- Support common GNSS systems represented in broadcast ephemeris data, including
  GPS, GLONASS, Galileo, BeiDou, QZSS, SBAS, and IRNSS/NavIC.
- Parse broadcast Keplerian ephemeris records.
- Parse GLONASS state-vector ephemeris records.
- Calculate satellite ECEF coordinates at a selected epoch.
- Display ephemeris records and calculated coordinates in a Windows Forms UI.
- Save or query calculation-related data through SQLite.

## Scientific and Teaching Use

This project is suitable for surveying, GNSS, geodesy, satellite navigation, and
mapping-programming coursework. It can be used to demonstrate the full workflow
from broadcast navigation ephemeris reading to satellite coordinate calculation.

Typical use cases include:

- GNSS satellite coordinate calculation experiments
- RINEX navigation file parsing practice
- GPS/BDS/Galileo broadcast ephemeris algorithm teaching
- GLONASS state-vector propagation demonstration
- comparison of satellite positions from different navigation records

## Project Structure

```text
.
├── core/
│   ├── RinexNavReader.cs          # RINEX navigation file reader
│   └── SatOrbitCalculator.cs      # Satellite coordinate calculation
├── data/
│   └── qliteHelper.cs             # SQLite helper
├── models/
│   ├── GnssEphemeris.cs           # Unified GNSS ephemeris model
│   ├── GPSEphemer.cs              # GPS ephemeris model
│   ├── GPSConst.cs                # GNSS constants
│   └── SatPositionResult.cs       # Coordinate calculation result model
├── Properties/                    # WinForms project resources/settings
├── Form1.cs                       # Main Windows Forms UI logic
├── Program.cs                     # Application entry point
├── GNSS卫星坐标计算.csproj          # Visual Studio C# project file
├── App.config
└── packages.config
```

## Requirements

- Windows
- Visual Studio 2019 or later recommended
- .NET Framework 4.7.2
- NuGet package restore enabled

NuGet dependencies:

- `System.Data.SQLite.Core`
- `Stub.System.Data.SQLite.Core.NetFramework`

## Build and Run

1. Open `GNSS卫星坐标计算.csproj` in Visual Studio.
2. Restore NuGet packages when prompted.
3. Build the project.
4. Run the WinForms application.
5. Open a RINEX navigation file from the UI and calculate satellite coordinates.

## Notes

- `bin/` and `obj/` are intentionally excluded from the repository package.
- The project targets .NET Framework, so it is intended for Windows desktop use.
- Some source files may contain Chinese UI text or comments. If characters are
  displayed incorrectly, open the files in Visual Studio and choose the original
  file encoding or resave them as UTF-8.

