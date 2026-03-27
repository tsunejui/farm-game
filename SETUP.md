# FarmGame Project Setup

This document describes the steps taken to set up the FarmGame project using MonoGame and .NET SDK managed by mise.

## Prerequisites

- [mise](https://mise.jdx.dev/) installed on the system

## Step 1: Configure mise for .NET SDK

Created a `.mise.toml` file in the project root to pin the .NET SDK version:

```toml
[tools]
dotnet = "9"
```

Then trusted the config and installed the SDK:

```bash
mise trust
mise install
```

This installed **.NET SDK 9.0.312**.

## Step 2: Install MonoGame Templates

Installed the official MonoGame C# project templates via the .NET CLI:

```bash
dotnet new install MonoGame.Templates.CSharp
```

This provides several templates including cross-platform desktop, Android, iOS, and starter kits.

## Step 3: Create the MonoGame Project

Created a cross-platform desktop application using the `mgdesktopgl` template:

```bash
dotnet new mgdesktopgl -n FarmGame
```

This generated the project under the `FarmGame/` directory with the following structure:

```
farm-game/
├── .mise.toml
├── FarmGame.sln
└── FarmGame/
    ├── .config/
    │   └── dotnet-tools.json
    ├── .vscode/
    │   └── launch.json
    ├── Content/
    │   └── Content.mgcb
    ├── FarmGame.csproj
    ├── Game1.cs
    ├── Program.cs
    ├── Icon.bmp
    ├── Icon.ico
    └── app.manifest
```

## Step 4: Create Solution File

Created a solution file and added the project to it:

```bash
dotnet new sln -n FarmGame
dotnet sln FarmGame.sln add FarmGame/FarmGame.csproj
```

## Step 5: Build and Verify

Built the solution to verify everything is set up correctly:

```bash
dotnet build FarmGame.sln
```

Build succeeded with 0 warnings and 0 errors.

## Running the Game

```bash
dotnet run --project FarmGame/FarmGame.csproj
```
