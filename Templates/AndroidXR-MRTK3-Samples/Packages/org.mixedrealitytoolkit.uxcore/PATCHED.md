# Patched copy of org.mixedrealitytoolkit.uxcore

Source: org.mixedrealitytoolkit.uxcore-3.3.0.tgz

Removed `Tests/`. Removed the class-level `[SerializeField]` on `LegacyDialog/DialogButton.cs`, which fails to compile (CS0592) on newer Unity 6000.3 patches.
Fixed upstream in MixedRealityToolkit/MixedRealityToolkit-Unity#1101 but not released yet. BSD 3-Clause, see LICENSE.md.
