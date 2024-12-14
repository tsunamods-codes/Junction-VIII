![License](https://img.shields.io/github/license/tsunamods-codes/Junction-VIII) ![Overall Downloads](https://img.shields.io/github/downloads/tsunamods-codes/Junction-VIII/total?label=Overall%20Downloads) ![Latest Stable Downloads](https://img.shields.io/github/downloads/tsunamods-codes/Junction-VIII/latest/total?label=Latest%20Stable%20Downloads&sort=semver) ![Latest Canary Downloads](https://img.shields.io/github/downloads/tsunamods-codes/Junction-VIII/canary/total?label=Latest%20Canary%20Downloads) ![GitHub Actions Workflow Status](https://github.com/tsunamods-codes/Junction-VIII/actions/workflows/main-1.1.0.yml/badge.svg?branch=master)

<div align="center">
  <img src="https://github.com/tsunamods-codes/Junction-VIII/blob/master/.logo/app.png" alt="">
  <br><small>Junction VIII is now officially part of the <a href="https://www.tsunamods.com/">Tsunamods</a> initiative!</small>
</div>

# Junction VIII

Mod manager for Final Fantasy VIII PC.

## Introduction

This is a fork of [7th Heaven](https://github.com/tsunamods-codes/7th-Heaven) adapted to Final Fantasy VIII, maintained now by the Tsunamods team.

## Download

- [Latest stable release](https://github.com/tsunamods-codes/Junction-VIII/releases/latest)
- [Latest canary release](https://github.com/tsunamods-codes/Junction-VIII/releases/tag/canary)

## Install

> **HINT:** For an easier experience you can also use the setup .exe file in the release which will take care of doing the following steps for you.

0. Download and install the latest [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) for BOTH x86 and x64
1. Download and install the latest [Microsoft Visual C++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2015-2017-2019-and-2022) for BOTH x86 and x64
2. Download the latest release using one of the links above
3. Extract the .zip file to your preferred location e.g. C:\Junction VIII.
4. Run `Junction VIII.exe`

## Build

### Preparation

0. Clone the [vcpkg](https://vcpkg.io) project in the root folder of your `C:` drive ( `git clone https://github.com/Microsoft/vcpkg.git` )
1. Go inside the `C:\vcpkg` folder and double click `bootstrap-vcpkg.bat`
2. Open a `cmd` window in `C:\vcpkg` and run the following command: `vcpkg integrate install`

### Visual Studio

0. Download the the latest [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/) installer
1. Run the installer and import this [.vsconfig](.vsconfig) file in the installer to pick the required components to build this project
2. Once installed, open the file [`JunctionVIII.sln`](JunctionVIII.sln) in Visual Studio and click the build button

### Visual Studio Code (Using Extension in Preview)

0. Make sure to have done the first two steps of **Visual Studio** section
1. Open VS Code and install the extension [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) (this will also install other dependent extensions)
2. Open the JunctionVIII folder and there will be a new tab in **Explorer**, called **Solution Explorer**, that contains a similar project explorer of Visual Studio
3. Build: right click on the solution **AppUI** and click on `Build`. Otherwise, run `dotnet build JunctionVIII.sln /target:AppUI`
4. Run: `dotnet run --project AppUI`
5. Debug: right click on the solution **AppUI** and click `Debug->Start New Instance`

## License

See [LICENSE.txt](LICENSE.txt)
