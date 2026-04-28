![Universal Scene Description for .NET](https://raw.githubusercontent.com/EggyStudio/UniversalSceneDescription/main/.github/assets/Header.png)

# Universal Scene Description for .NET

[![NuGet](https://img.shields.io/nuget/v/UniversalSceneDescription.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/UniversalSceneDescription)
[![Downloads](https://img.shields.io/nuget/dt/UniversalSceneDescription.svg?logo=nuget&label=Downloads)](https://www.nuget.org/packages/UniversalSceneDescription)
[![License: MPL-2.0](https://img.shields.io/badge/License-MPL--2.0-brightgreen.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-win--x64%20%7C%20linux--x64%20%7C%20osx--x64%20%7C%20osx--arm64-informational)](#platform-support)

Pixar's [**OpenUSD**](https://openusd.org/) - Universal Scene Description - packaged
as a single, drop-in NuGet for **.NET 10**. Bundles the managed bindings, the native
USD runtime, and the Pixar plugin tree under the standard `runtimes/<rid>/native/`
NuGet convention, so the .NET SDK automatically deploys everything into your
output folder. No build steps, no environment variables, no plugin
registration code.

> USD is a high-performance extensible software platform for collaboratively
> constructing animated 3D scenes, designed to meet the needs of large-scale film
> and visual effects production.

---

## Install

```bash
dotnet add package UniversalSceneDescription
```

## Quick start

```csharp
using UniversalSceneDescription;
using pxr;

// Configures the native loader and registers the bundled Pixar plugin tree.
// Idempotent and thread-safe - call once at startup.
UsdRuntime.Initialize();

using var stage = UsdStage.CreateNew("hello.usda");
UsdGeomXform.Define(stage, new SdfPath("/Hello"));
UsdGeomSphere.Define(stage, new SdfPath("/Hello/World"));
stage.Save();
```

That's it. The `UsdRuntime.Initialize()` call points USD at the `usd/` plugin
folder that the .NET SDK dropped next to your executable, and makes sure the OS
loader can resolve the native DLLs.

## What's in the box

| Component | Source | Where it lands                        |
|---|---|---------------------------------------|
| Managed `USD.NET.dll` (`pxr.*` SWIG bindings) | Unity `com.unity.usd.core` | `lib/net10.0/`                         |
| Native USD runtime (`usd_*`, `UsdCs`, `tbb`, `Imath`, `Alembic`, per platform extension) | Pixar OpenUSD (Unity `com.unity.usd.core` distribution) | `runtimes/<rid>/native/` |
| Pixar plugin tree (`plugInfo.json` + generated schemas for `usdGeom`, `usdShade`, `usdSkel`, `usdLux`, `usdPhysics`, `usdRender`, …) | Pixar OpenUSD | `runtimes/<rid>/native/usd/` |
| `UniversalSceneDescription.UsdRuntime` helper | this package | `UniversalSceneDescription` namespace |

The .NET SDK auto-deploys everything under `runtimes/<rid>/native/` to your build
output (preserving the `usd/` subdirectory layout) - `dotnet run`, `dotnet test`,
and `dotnet publish` all just work.

## Configuration

Override the plugin location at runtime:

```csharp
UsdRuntime.Initialize(
    pluginPath: "/custom/path/to/usd",
    nativeLibraryDirectory: "/custom/path/to/native");
```

To suppress the bundled native runtime entirely (rare - only useful if you ship
your own USD build):

```xml
<PackageReference Include="UniversalSceneDescription" Version="7.1.0">
  <ExcludeAssets>runtime</ExcludeAssets>
</PackageReference>
```

Opt out of the automatic plugin-tree staging (rare - only needed if you
want to stage the `usd/` plugin folder somewhere other than next to the
executable, or you ship your own USD build):

```xml
<PropertyGroup>
  <DisableUsdPluginStaging>true</DisableUsdPluginStaging>
</PropertyGroup>
```

## Platform support

| Runtime identifier | Status | Native asset folder |
|---|---|---|
| `win-x64`   | ✅ Shipping              | `runtimes/win-x64/native/`   |
| `linux-x64` | ✅ Shipping | `runtimes/linux-x64/native/` |
| `osx-x64`   | ✅ Shipping | `runtimes/osx-x64/native/`   |
| `osx-arm64` | ✅ Shipping | `runtimes/osx-arm64/native/` |

The csproj globs `runtimes/**/*`, so refreshing any per-RID folder with a newer
platform USD build is enough to ship updated native support - no csproj edit
needed. Each per-RID folder includes runtime notes and currently ships binaries
from Unity's `com.unity.usd.core` plugin distribution.

`UsdRuntime` itself is fully cross-platform: on Linux it prepends the native
directory to `LD_LIBRARY_PATH`, on macOS to `DYLD_LIBRARY_PATH` and
`DYLD_FALLBACK_LIBRARY_PATH`, on Windows to `PATH`. `PXR_PLUGINPATH_NAME` is
set on every platform so libplug discovers the bundled plugin tree.

## Learn USD

- [Introduction to USD](https://openusd.org/release/intro.html)
- [Tutorials](https://openusd.org/release/tut_usd_tutorials.html)
- [FAQ](https://openusd.org/release/usdfaq.html)
- [Toolset](https://openusd.org/release/toolset.html)
- [NVIDIA USD resources & samples](https://developer.nvidia.com/usd)
- [Pixar demo assets](https://openusd.org/release/dl_downloads.html#assets)

## Credits

- **Pixar Animation Studios** - authors of [OpenUSD](https://openusd.org/).
- **Unity Technologies** - the managed C# bindings vendored here are extracted
  from the [`com.unity.usd.core`](https://docs.unity3d.com/Packages/com.unity.usd.core@1.0/manual/index.html) package.
- **3DEngine** - packaging and .NET 10 helper API.

## License

[MPL-2.0](LICENSE) - see file for details. OpenUSD itself is licensed under the
[Modified Apache 2.0 License](https://openusd.org/release/LICENSE).

---

Issues, PRs and contributions welcome at
[github.com/EggyStudio/UniversalSceneDescription](https://github.com/EggyStudio/UniversalSceneDescription).

