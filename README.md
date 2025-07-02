# Mid-air screen control using mouse driven by a body tracker

The application takes the hand tip joint from a body tracker sensor and uses it to perform the following operations on a mid-air screen (like a fog screen) located at a certain distance in front of the tracker with the screen projected there:
- move the mouse cursor,
- generate mouse left button up/down events when the hand tip (finger) crosses the screen surface.

## Supported devices

- Kinect for Windows 2.0

## Compilation

To compile the project, you will have to install
- VS Visual Studio 2019 with .NET Framework 4.8 
- [Kinect 2.0 SDK](https://www.microsoft.com/en-us/download/details.aspx?id=44561).

## Installation

Download the [installation package](https://github.com/lexasss/FogControlWithKinect/releases) and run the executable file.

If the application complains about MS .NET Framework 4.8 missing in the system. Install the [.NET Framework runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) before trying again.

To use Kinect 2.0 as a body tracker, install [Kinect 2.0 runtime](https://www.microsoft.com/en-us/download/details.aspx?id=44559) to the target PC.
