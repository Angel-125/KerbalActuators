# KerbalActuators (unofficial fix fork)

Fork of [Angel-125/KerbalActuators](https://github.com/Angel-125/KerbalActuators) by Michael Billard. All credit for the mod goes to him — this fork exists only because the original hasn't seen a commit since mid-2024 and I hit a reproducible crash.

## What's different

`WBIVTOLManager.Start()` used to read `FlightGlobals.ActiveVessel` immediately. That property falls back to `Object.FindObjectOfType(typeof(FlightGlobals))` when its cached instance is stale, and calling that on the same frame the Flight scene's addons are still being instantiated can crash the game (`Access Violation` inside `UnityEngine.Object.FindObjectsOfType`). Now it waits a frame via a coroutine before touching it.

See `KerbalActuators/Managers/WBIVTOLManager.cs`.

## License

GPLv3, same as upstream.
