using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UniversalSceneDescription;

/// <summary>
/// One-call initializer for the bundled OpenUSD native runtime.
/// </summary>
/// <remarks>
/// <para>
/// The <c>UniversalSceneDescription</c> NuGet package ships:
/// </para>
/// <list type="bullet">
///   <item><description>The managed <c>USD.NET.dll</c> assembly (Pixar SWIG bindings, <c>pxr.*</c> namespace).</description></item>
///   <item><description>Per-RID native USD runtime libraries under <c>runtimes/&lt;rid&gt;/native/</c>.</description></item>
///   <item><description>The Pixar plugin tree (<c>usd/</c> with <c>plugInfo.json</c> + generated schemas).</description></item>
/// </list>
/// <para>
/// The .NET SDK auto-deploys the per-RID files into your build output. This
/// helper then sets <c>PXR_PLUGINPATH_NAME</c> so libplug discovers the bundled
/// plugin tree, and prepends the native directory to the platform's library
/// search environment variable so transitive dependencies resolve correctly.
/// </para>
/// <para>
/// Cross-platform: works on <b>Windows</b>, <b>Linux</b>, and <b>macOS</b>
/// (provided the matching native binaries are present under
/// <c>runtimes/&lt;rid&gt;/native/</c>). Initialization is idempotent and
/// thread-safe — call it once at program startup, before touching any
/// <c>pxr.*</c> type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using UniversalSceneDescription;
/// using pxr;
///
/// UsdRuntime.Initialize();
///
/// using var stage = UsdStage.CreateNew("hello.usda");
/// UsdGeomXform.Define(stage, new SdfPath("/Hello"));
/// stage.Save();
/// </code>
/// </example>
public static class UsdRuntime
{
    private const string PxrPluginPathEnvVar = "PXR_PLUGINPATH_NAME";
    private const string PluginFolderName = "usd";

    private static readonly object InitLock = new();

    /// <summary>
    /// Gets a value indicating whether <see cref="Initialize"/> has completed
    /// successfully in this process.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the absolute path of the USD plugin tree that
    /// <see cref="Initialize"/> registered with the native runtime, or
    /// <see langword="null"/> if initialization has not yet been performed.
    /// </summary>
    public static string? PluginPath { get; private set; }

    /// <summary>
    /// Gets the directory the native USD libraries are expected to be loaded
    /// from. Defaults to <see cref="AppContext.BaseDirectory"/> (the consumer
    /// application's output folder), where the .NET SDK deploys per-RID
    /// runtime assets.
    /// </summary>
    public static string NativeLibraryDirectory { get; private set; } = AppContext.BaseDirectory;

    /// <summary>
    /// Initializes the OpenUSD runtime: ensures native libraries are
    /// discoverable and registers the bundled Pixar plugin tree.
    /// </summary>
    /// <param name="pluginPath">
    /// Optional absolute path to a USD plugin folder. When <see langword="null"/>,
    /// the <c>usd</c> folder next to the running assembly
    /// (<see cref="AppContext.BaseDirectory"/>) is used.
    /// </param>
    /// <param name="nativeLibraryDirectory">
    /// Optional directory containing the native USD libraries. When
    /// <see langword="null"/>, <see cref="AppContext.BaseDirectory"/> is used.
    /// </param>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the resolved plugin folder does not exist on disk.
    /// </exception>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when running on an OS for which no library search environment
    /// variable is recognised.
    /// </exception>
    /// <remarks>
    /// Subsequent calls are no-ops once the runtime has been initialized
    /// successfully.
    /// </remarks>
    public static void Initialize(string? pluginPath = null, string? nativeLibraryDirectory = null)
    {
        if (IsInitialized)
            return;

        lock (InitLock)
        {
            if (IsInitialized)
                return;

            NativeLibraryDirectory = nativeLibraryDirectory is { Length: > 0 }
                ? Path.GetFullPath(nativeLibraryDirectory)
                : AppContext.BaseDirectory;

            string resolvedPluginPath = pluginPath is { Length: > 0 }
                ? Path.GetFullPath(pluginPath)
                : Path.Combine(NativeLibraryDirectory, PluginFolderName);

            if (!Directory.Exists(resolvedPluginPath))
            {
                throw new DirectoryNotFoundException(
                    $"USD plugin directory not found: '{resolvedPluginPath}'. " +
                    "Verify the UniversalSceneDescription package has deployed " +
                    "its native assets to the application output folder for the " +
                    $"current runtime identifier ('{RuntimeInformation.RuntimeIdentifier}').");
            }

            EnsureNativeSearchPath(NativeLibraryDirectory);
            RegisterPluginPath(resolvedPluginPath);

            PluginPath = resolvedPluginPath;
            IsInitialized = true;
        }
    }

    /// <summary>
    /// Ensures <see cref="Initialize"/> has been called, using all defaults.
    /// </summary>
    public static void EnsureInitialized() => Initialize();

    private static void EnsureNativeSearchPath(string nativeDirectory)
    {
        // Prepend to the platform's loader search path so transitive
        // dependencies of the USD libraries (tbb, Imath, …) resolve from the
        // same directory. The .NET runtime already searches AppBase first for
        // direct P/Invoke calls, so the directly-imported libraries are
        // usually fine without this — but their inter-library dependencies
        // are loaded by the OS loader, which honours these env vars on POSIX
        // systems and PATH on Windows.
        string searchPathVar =
            OperatingSystem.IsWindows() ? "PATH" :
            OperatingSystem.IsMacOS()   ? "DYLD_LIBRARY_PATH" :
            OperatingSystem.IsLinux()   ? "LD_LIBRARY_PATH" :
            throw new PlatformNotSupportedException(
                "Unrecognised operating system. Supply your own native USD build via " +
                "UsdRuntime.Initialize(pluginPath, nativeLibraryDirectory).");

        PrependToEnvironmentPath(searchPathVar, nativeDirectory);

        // On macOS additionally seed the fallback search path — DYLD_LIBRARY_PATH
        // is stripped from hardened/SIP-protected processes, so the fallback
        // is often the only one that survives.
        if (OperatingSystem.IsMacOS())
            PrependToEnvironmentPath("DYLD_FALLBACK_LIBRARY_PATH", nativeDirectory);
    }

    private static void RegisterPluginPath(string pluginPath)
    {
        // PXR_PLUGINPATH_NAME is read by libplug the first time PlugRegistry
        // is touched. Setting it before the first managed pxr.* call is the
        // most reliable, fully cross-platform way to register plugins without
        // taking a hard compile dependency on the SWIG-generated API surface.
        PrependToEnvironmentPath(PxrPluginPathEnvVar, pluginPath);
    }

    private static void PrependToEnvironmentPath(string variableName, string directory)
    {
        string current = Environment.GetEnvironmentVariable(variableName) ?? string.Empty;
        if (current.Split(Path.PathSeparator).Contains(directory, StringComparer.OrdinalIgnoreCase))
            return;

        string combined = string.IsNullOrEmpty(current)
            ? directory
            : $"{directory}{Path.PathSeparator}{current}";
        Environment.SetEnvironmentVariable(variableName, combined);
    }
}

