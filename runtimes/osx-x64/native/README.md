# macOS x64 native runtime

This folder is populated and shipped for the `osx-x64` runtime identifier.

Current binaries are sourced from Unity's `com.unity.usd.core` plugin
distribution (`Runtime/Plugins/x86_64/MacOS/lib`). It includes native USD
libraries (`libusd_*.dylib`), `libUsdCs.dylib`, supporting dependencies, and
the bundled Pixar plugin tree under `usd/`.

When updating to a newer OpenUSD build, keep install names relative to
`@loader_path` (or rewrite with `install_name_tool`) so transitive dylib
resolution works in published app layouts:

    -install_name @loader_path/<lib>.dylib


