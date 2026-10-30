#!/bin/bash
# Hinoki build script — compiles the WPF tray app with the in-box C# compiler.
# The result runs on any Windows 10/11 machine with zero dependencies (.NET Framework 4.8 is built into the OS).
set -e
cd "$(dirname "$0")"

CSC="C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
FW="C:/Windows/Microsoft.NET/Framework64/v4.0.30319"

mkdir -p ../build

# csc wants Windows-style backslash paths
SRCS=$(ls src/*.cs | sed 's|/|\\|g')

"$CSC" -nologo -target:winexe -platform:anycpu -optimize+ \
  -out:../build/Hinoki.exe \
  -win32icon:assets/hinoki.ico \
  -win32manifest:app.manifest \
  -resource:assets/logo.png,Hinoki.logo.png \
  -resource:assets/hinoki.ico,Hinoki.hinoki.ico \
  -r:"$FW/WPF/PresentationCore.dll" \
  -r:"$FW/WPF/PresentationFramework.dll" \
  -r:"$FW/WPF/WindowsBase.dll" \
  -r:"$FW/System.Xaml.dll" \
  -r:"$FW/System.Windows.Forms.dll" \
  -r:"$FW/System.Drawing.dll" \
  -r:"$FW/System.Web.Extensions.dll" \
  $SRCS

echo "---- BUILD OK ----"
ls -la ../build/Hinoki.exe
