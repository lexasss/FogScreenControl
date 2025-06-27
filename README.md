# Mouse control on a mid-air screen

The application takes the hand tip joint from a body tracker sensor and uses it to perform the following operations on a mid-air screen (like a fog screen) located at a certain distance in front of the tracker with the screen projected there:
- move the mouse cursor,
- generate mouse left button up/down events when the hand tip (finger) crosses the screen surface.

Note that a file with screen calibration data must exits in the app folder. Create the calibration file by running the screen calibration procedure (by June 2025, this is a separate software soon to be integrated with this application).

## Supported devices

- Kinect for Windows 2.0

## Compilation

To compile the project, you will have to install
- VS Visual Studio 2019 with .NET Framework 4.8 
- [Kinect 2.0 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=44561).

## Installation

The application is distributed in a portable setup and does not require installation. Simply extract the [archive](https://github.com/lexasss/FogControlWithKinect/releases) and run the executable file.

On the first run, the application may complain about MS .NET Framework 4.8 missing in the system. Install the [.NET Framework runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) before trying again.

If Kinect 2.0 SDK was not installed, then [Kinect 2.0 runtime](https://www.microsoft.com/en-us/download/details.aspx?id=44559) should be installed on the target PC.
