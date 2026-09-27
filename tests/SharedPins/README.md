# Shared pin tests

Run `dotnet run --project tests/SharedPins/SharedPins.Tests.csproj -c Release` from the repository root with .NET SDK 9.

The project links the production pin store, RPC service and UI hooks directly. The game/Unity/BepInEx/Harmony APIs are controlled doubles in Stubs.cs. Separate AssemblyLoadContexts hold independent server and client static state; the runner delivers serialized RPC messages between them. Tests create uniquely named scratch directories under artifacts/shared-pin-tests. The disk tests exercise File.Replace; a restricted Windows sandbox may prevent that operation.

These tests cover logic and protocol behavior; they do not prove the Harmony hooks or UI work inside Unity. Use the multiplayer checklist in docs/shared-pins.md before treating the build as stable.
