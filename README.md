# Gravity Simulator

Small 2D gravity simulator.
Made using raylib.
Was inspired by [this video](https://youtu.be/ryfbBB3pHfI).

I made it for fun.
Simulation is done 1 step/frame (no `deltaTime`).

You can:
- pause it,
- simulate 1 step at a time,
- move around,
- launch new particles,
- follow the Sun (the biggest particle),
- toggle trails between Sun space and absolute space,
- restart it,
- toggle HUD and debug info.

The trickiest part was implementing trails (especially for dead particles).

## How to check it out

1. Make sure you have [.NET Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (or higher version) installed. Run `dotnet --info` in terminal to check this.
2. Go to [releases](https://github.com/bigchunguspng/gravity-sim/releases) and download archive for your OS.
3. Unpack it anywhere you like and launch the app.

## Screenshorts

![Screenshot 2026-08-28 154034](https://github.com/user-attachments/assets/2be85ce5-3cb2-40dd-81d0-77f24868fc1e)
![Screenshot 2026-08-22 121503](https://github.com/user-attachments/assets/631f30f1-a18c-44eb-82f0-b08aca6145b5)
![Screenshot 2026-08-22 121542](https://github.com/user-attachments/assets/3feb8667-8580-4461-bee0-5e31230f4683)

### Funny bugs)))00

![Screenshot 2026-08-27 115458](https://github.com/user-attachments/assets/8b37e33b-1e04-42a3-ac91-f30fd144b1dc)
![Screenshot 2026-08-27 115437](https://github.com/user-attachments/assets/c9b397cb-c2bb-428d-8042-11d2faa9a809)
