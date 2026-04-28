# Linux x64 native runtime

This folder is populated and shipped for the `linux-x64` runtime identifier.

Current binaries are sourced from Unity's `com.unity.usd.core` plugin
distribution (`Runtime/Plugins/x86_64/Linux/lib`). It includes the native USD
libraries (`libusd_*.so`), `libUsdCs.so`, supporting dependencies (`libtbb`,
`libImath`, `libAlembic`), and the bundled Pixar plugin tree under `usd/`.

When updating to a newer OpenUSD build, keep the same layout and ensure shared
libraries are built with `$ORIGIN` rpath so transitive dependencies resolve from
the deployed output directory:

    -Wl,-rpath,'$ORIGIN'


