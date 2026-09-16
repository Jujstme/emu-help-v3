using EmuHelp.HelperBase;
using System;

namespace EmuHelp.Systems.GBC;

public abstract class GBCEmulator : Emulator
{
    // SRAM (or External RAM) is located at 0xA000-0xBFFF in the GBC memory map.
    // This RAM area can be read from and written to normally. Some MBCs allow mapping
    // of other hardware into the RAM area in this way.
    // This cartridge RAM is often battery buffered to hold saved game positions,
    // high score tables, and other information when the Game Boy is turned off.
    public IntPtr SRAM { get; protected set; }

    // Work RAM (WRAM) is located at 0xC000-0xDFFF in the GBC memory map. This RAM area
    // is internal to the GBC and can be read from and written to normally.
    // It is technically mirrored at 0xE000-0xFDFF, but direct access into this
    // area is prohibited according to Nintendo's specifications.
    public IntPtr WRAM { get; protected set; }

    // IO registers and High RAM (HRAM) are located at 0xFF00-0xFFFE in the GBC memory map.
    // 0xFFFF is reserved for the interrupt vector.
    public IntPtr IOHRAM { get; protected set; }
}