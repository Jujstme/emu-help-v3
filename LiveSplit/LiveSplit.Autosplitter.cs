// Inspired by asl-help, by just-ero.
// asl-help is licensed under GPL-3.0.
// See: https://github.com/just-ero/asl-help

using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using Irony.Parsing;
using LiveSplit.ASL;
using LiveSplit.UI.Components;
using Helper.Common.Reflection;

namespace Helper.LiveSplit;

/// <summary>
/// Static class that handles the integration of the autosplitter with LiveSplit.
/// It manages the ASL script and its methods.
/// </summary>
internal static class Autosplitter
{
    /// <summary>
    /// The ASL script currently being executed.
    /// </summary>
    private static ASLScript ASLScript { get; }

    /// <summary>
    /// The methods defined in the ASL script.
    /// </summary>
    internal static ASLScript.Methods ASLMethods { get; }

    /// <summary>
    /// An instance of the Actions class that defines various game event actions.
    /// </summary>
    public static Actions Actions { get; }

    /// <summary>
    /// Gets the <see cref="Process"/> currently hooked by <see cref="LiveSplit"/>.
    /// </summary>
    public static Process Game => ASLScript.GetFieldValue<Process>("_game");

    /// <summary>
    /// The <see langword="vars"/> <see cref="ExpandoObject"/> used inside an autosplitter .asl.
    /// </summary>
    public static IDictionary<string, dynamic> Vars { get; }

    /// <summary>
    /// The <see langword="current"/> <see cref="ExpandoObject"/> used inside an autosplitter .asl
    /// </summary>
    public static IDictionary<string, dynamic> Current => ASLScript.State.Data;

    /// <summary>
    /// The <see langword="old"/> <see cref="ExpandoObject"/> used inside an autosplitter .asl
    /// </summary>
    public static IDictionary<string, dynamic> Old => ASLScript.OldState.Data;

    /// <summary>
    /// Static constructor for the Autosplitter class.
    /// Initializes the autosplitter by loading the ASL script and setting up its methods and actions.
    /// </summary>
    static Autosplitter()
    {
        // Retrieve the assembly of the compiled ASL script by examining the stack trace.
        Assembly compiledScriptAssembly = new StackTrace()
            .GetFrames()
            .Select(frame => frame.GetMethod().DeclaringType)
            .First(decl => decl is not null && decl.Name == "CompiledScript")
            .Assembly;

        // Get the ASLComponent instances from the LiveSplit timer layout.
        IEnumerable<ASLComponent> components = Timer.Layout.Components
            .Prepend(Timer.Run.AutoSplitter?.Component)
            .OfType<ASLComponent>();

        // Find the relevant ASLComponent based on the compiled script assembly.
        ASLComponent aslComponent = components.FirstOrDefault(component =>
        {
            ASLScript script = component.Script;
            ASLScript.Methods methods = script.GetFieldValue<ASLScript.Methods>("_methods");

            return !methods.startup.IsEmpty &&
                methods.startup.GetFieldValue<object>("_compiled_code").GetType().Assembly == compiledScriptAssembly;
        });

        // Initialize properties with the ASL script and its methods.
        ASLScript = aslComponent.Script;
        Vars = ASLScript.Vars;
        ASLMethods = ASLScript.GetFieldValue<ASLScript.Methods>("_methods");
        Actions = new Actions();

        // Load the ASL script file for parsing.
        ComponentSettings settings = aslComponent.GetFieldValue<ComponentSettings>("_settings");
        string path = settings.ScriptPath;
        string code = File.ReadAllText(path);

        // Parse the ASL code using Irony.
        ASLGrammar aslGrammar = new(); 
        Parser parser = new(aslGrammar);
        ParseTree tree = parser.Parse(code);
        ParseTreeNode methodsNode = tree.Root.ChildNodes.First(n => n.Term.Name == "methodList");

        // Iterate through the parsed methods and store their information in the Actions instance.
        foreach (ParseTreeNode method in methodsNode.ChildNodes[0].ChildNodes)
        {
            // Extract the method name and body from the parse tree.
            if (method.ChildNodes[0].Token.Value is not string name || method.ChildNodes[2].Token.Value is not string body)
                continue;

            int line = method.ChildNodes[2].Token.Location.Line + 1;

            // Map each method to its corresponding action in the Actions instance.
            switch (name)
            {
                case ASLMethodNames.Startup: Actions.Startup = new(body, name, line); break;
                case ASLMethodNames.Shutdown: Actions.Shutdown = new(body, name, line); break;
                case ASLMethodNames.Init: Actions.Init = new(body, name, line); break;
                case ASLMethodNames.Exit: Actions.Exit = new(body, name, line); break;
                case ASLMethodNames.Update: Actions.Update = new(body, name, line); break;
                case ASLMethodNames.Start: Actions.Start = new(body, name, line); break;
                case ASLMethodNames.Split: Actions.Split = new(body, name, line); break;
                case ASLMethodNames.Reset: Actions.Reset = new(body, name, line); break;
                case ASLMethodNames.GameTime: Actions.GameTime = new(body, name, line); break;
                case ASLMethodNames.IsLoading: Actions.IsLoading = new(body, name, line); break;
                case ASLMethodNames.OnStart: Actions.OnStart = new(body, name, line); break;
                case ASLMethodNames.OnSplit: Actions.OnSplit = new(body, name, line); break;
                case ASLMethodNames.OnReset: Actions.OnReset = new(body, name, line); break;
            }
        }
    }
}

internal static class ASLMethodNames
{
    public const string Startup = "startup";
    public const string Shutdown = "shutdown";
    public const string Init = "init";
    public const string Exit = "exit";
    public const string Update = "update";
    public const string Start = "start";
    public const string Split = "split";
    public const string Reset = "reset";
    public const string GameTime = "gameTime";
    public const string IsLoading = "isLoading";
    public const string OnStart = "onStart";
    public const string OnSplit = "onSplit";
    public const string OnReset = "onReset";
}
