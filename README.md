# FSharp-Suave-Expecto

This a a quick reference app for using F#, Suave and Expecto together that works in VS Code.

## Features

- HTTP API for a "document store" API
- Suave Self Hosted Web Server
- Expecto Tests
  - Simple Tests
  - Integration Tests against the actual webserver
- FAKE build script

## Building

From a command prompt with F# 4.1+ available from the default path:

Run `build.cmd Test` (or `build.sh Test`) to build and run all the tests.

## Developing

Open VS Code in the root directory and press `Ctrl+F5` to run the FAKE build to build just the code without the tests.

You can run the tests in VS Code by Pressing `Ctrl+F6` to run the Expecto tests.

To make VS Code run the tests and build, change `"Build"` on this line in `build.fxs` to `"Test"`:

```f#
// start build
RunTargetOrDefault "Build"
```

## Pre-requisites

- F# 4.1
- VS Code
 - Ionide-FAKE
 - Ionide-fsharp
 - Ionide-Paket

## Help

If you have any problems running the code / tests or you want to contribute any changes, or
if you just want to ask about anything else, feel free to open an issue.
