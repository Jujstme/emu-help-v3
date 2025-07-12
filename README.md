# emu-help

emu-help is a C# library intended to provide easy access to memory addresses in console games being run via emulators.
Its primary intended use case is in conjunction with a .asl script for LiveSplit autosplitters on Windows systems.

## Supported systems and emulators

Support is currently provided for the following systems:
- Microsoft Xbox 360
- Nintendo Game Boy Advance
- Nintendo Game Boy Color
- Nintendo GameCube
- Nintendo Wii
- Sega Master System
- Sega Mega Drive / Sega Genesis
- Sony PlayStation 1
- Sony PlayStation 2
- Sony PlayStation Portable

Each system has separate support for various emulators. For example, Sega Genesis games are supported on Retroarch, Gens, BlastEm, Fusion / Kega Fusion and the official Steam release of SEGA Genesis Classics. Or, just to make another example, Playstation 1 games are supported on Retroarch, ePSXe, PCSX_Redux, Xebra, pSX and Duckstation.

Supported emulators for each system can be inferred by looking at the source code. In case of need, please tag Jujstme in the `#auto-splitters` channel of the Speedrun Tool Development Discord server: https://discord.gg/cpYsxz7.

## Examples

The following example creates a persistent instance of the class of your choice. In this example we'll pretend we want to write an autosplitter for a PS1 game.
The library will automatically generate some code needed for the autosplitter to work and then load the new class instance as `vars.Helper`.

```cs
state("LiveSplit") {}

startup
{
    Assembly.Load(File.ReadAllBytes("Components/emu-help-v3")).CreateInstance("PS1");
}
```

## Defining memory addresses

Our values of interest can easily be recovered by knowing the mapped RAM addresses in the original system's memory.
In most cases, the following example will be enough:

```cs
startup
{
    ...
    vars.IGT = vars.Helper.Make<int>(0x800A36A0);
    vars.Lives = vars.Helper.Make<byte>(0x8002AAC);
    vars.Map = vars.Helper.MakeString(15, 0x800B6000);
    vars.ObjectiveFlags = vars.Helper.MakeArray<byte>(0x20, 0x800B6304);
}
```

In case of systems using pointers (eg. PS2 or Wii), you can easily provide the entire pointer path. The library will automatically dereference the pointer path for you:

```cs
startup
{
    ...
    vars.IGT = vars.Helper.Make<int>(0x800A36A0, 0x20, 0xC0);
    vars.Lives = vars.Helper.Make<byte>(0x8002AAC);
    vars.Map = vars.Helper.MakeString(15, 0x800B2010, 0x34);
}
```

You can then use your values directly in your code:

```cs
update
{
    print(current.IGT.ToString());
    print(vars.ObjectiveFlags.Current[0].ToString());
}

split
{
    return vars.Map.Current != vars.Map.Old;
}
```

It is also possible to directly perform a memory read in the memory of the emulated system:

```cs
update
{
    // Example
    int lives = vars.Helper.Read<int>(0x80002AAC);
    ...
    print(lives.ToString());
}
```

## Endianess

Some systems, mostly older ones and especially Nintendo's, use and store memory values as Big Endian.

For example, values in memory might get stored as the following:
```
    00 00 00 01  // Nintendo Gamecube (Big Endian)
    01 00 00 00  // PC (Little Endian)
```

The library is programmed to automatically convert big-endian values to little-endian, for supported emulators, when applicable.


