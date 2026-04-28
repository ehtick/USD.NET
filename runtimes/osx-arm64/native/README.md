# macOS arm64 (Apple Silicon) native runtime

This folder is populated and shipped for the `osx-arm64` runtime identifier.

Current binaries are sourced from Unity's `com.unity.usd.core` plugin
distribution (`Runtime/Plugins/arm64/MacOS/lib`). It includes native USD
libraries, `libUsdCs.dylib`, supporting dependencies, and the bundled Pixar
plugin tree under `usd/`.

Use the same update guidance as `runtimes/osx-x64/native/README.md`, but with
arm64 builds.

