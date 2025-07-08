using System;
using EmuHelp.Logging;
using System.Diagnostics;
#if LIVESPLIT
using EmuHelp.LiveSplit;
#endif

namespace EmuHelp.HelperBase;

/// <summary>
/// Base class for helpers that interact with emulators. This abstract class
/// contains the core logic for code generation, assembly resolution, 
/// and updating emulator memory addresses.
/// </summary>
public abstract partial class HelperBase : IDisposable
{
#if LIVESPLIT
    private readonly bool isASLCodeGenerating;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelperBase"/> class with code generation enabled by default.
    /// </summary>
    public HelperBase()
        : this(true) { }
#endif

#if LIVESPLIT
    /// <summary>
    /// Initializes a new instance of the <see cref="HelperBase"/> class, optionally enabling code generation.
    /// Code generation should be disabled if the helper is used in more advanced .asl scripts.
    /// </summary>
    /// <param name="generateCode">A boolean indicating whether code generation is enabled.</param>
    public HelperBase(bool generateCode)
#else
    /// <summary>
    /// Initializes a new instance of the <see cref="HelperBase"/> class
    /// </summary>
    public HelperBase()
#endif
    {
#if LIVESPLIT
        using (Process thisProcess = Process.GetCurrentProcess())
        {
            if (!thisProcess.ProcessName.Equals("livesplit", StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidProgramException("This helper can be initialized only on LiveSplit.");
        }

        // Ensure the helper is instantiated in the 'startup {}' action
        if (Actions.CurrentAction != ASLMethodNames.Startup)
            throw new InvalidOperationException("The helper may only be instantiated in the 'startup {}' action.");

        // Log welcome messages
        Log.Welcome();

        // Subscribe to the AssemblyResolve event to handle assembly loading
        LiveSplitAssembly.AssemblyResolveSubscribe();
        try
        {
            // Manually invoke the static constructor for the static Autosplitter class.
            // This allows us to access the script and LiveSplit data even without
            // code generation.
            typeof(Autosplitter).TypeInitializer.Invoke(null, null);
        }
        finally
        {
            // Unsubscribe from the AssemblyResolve event
            LiveSplitAssembly.AssemblyResolveUnsubscribe();
        }

        isASLCodeGenerating = generateCode;
            if (isASLCodeGenerating)
            {
                Log.Info("Loading emu-help...");
                Log.Info("  => Generating code...");
                string helperName = "Helper";
                Autosplitter.Vars[helperName] = this;
                Log.Info($"    => Set helper to vars.{helperName}.");
                Autosplitter.Actions.Update.Prepend($"if (!vars.{helperName}.Update()) return false;");
                Autosplitter.Actions.Shutdown.Append($"vars.{helperName}.Dispose();");
                Autosplitter.Actions.Exit.Append($"vars.{helperName}.Exit();");
            }
            else
            {
                // Log messages when code generation is disabled
#endif
                Log.Info("Loading helper...");
#if LIVESPLIT
            }
#endif
    }

    /// <summary>
    /// Runs internal logic to hook into the target emulator and update its memory addresses.
    /// </summary>
    /// <returns>true if the target process is successfully hooked and the emulated RAM address is found,
    /// false otherwise.</returns>
    public virtual bool Update()
    {
        try
        {
            // Check if the emulator process is hooked, if not try to hook it
            if (emulatorProcess is null)
            {
                TryProcessHook();

                // If emulator process is still null, return false
                if (emulatorProcess is null)
                    return false;
            }

            // Check if the emulator process has exited
            if (!emulatorProcess.IsOpen)
            {
                HandleProcessExit();
                return false;
            }

            // Attach the emulator class if it hasn't been done yet
            emulatorClass ??= AttachEmuClass();

            // If the emulator class is still null, return false
            if (emulatorClass is null)
                return false;

            // If the emulator has not found the RAM, attempt to find it
            if (!emulatorClass.FoundRam)
            {
                if (emulatorClass.FindRAM(emulatorProcess))
                    emulatorClass.FoundRam = true; // Update FoundRam status if RAM is found
                else
                    return false; // Return false if RAM was not found
            }

            if (!emulatorClass.KeepAlive(emulatorProcess))
            {
                emulatorClass.FoundRam = false; // Update FoundRam status if keepAlive fails
                return false;
            }

            _tickCounter.Tick();

            // Return true indicating a successful update
            return true;
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during the update process
            Log.Info(ex);
            if (ex.InnerException is not null)
                Log.Info($"{ex.InnerException}");

            return false; // Return false if an exception occurs
        }
    }

    /// <summary>
    /// Code to execute when the autosplitter exits form its hooked process.
    /// </summary>
    public virtual void Exit()
    {
        Dispose();
    }

    /// <summary>
    /// Disposes of the resources used by the helper.
    /// </summary>
    public virtual void Dispose()
    {
        emulatorProcess?.Dispose();
        emulatorProcess = null;
    }
}