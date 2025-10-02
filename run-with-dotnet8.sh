#!/bin/bash
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
cd "$(dirname "$0")"
/opt/homebrew/opt/dotnet@8/bin/dotnet run --urls="http://localhost:5050;https://localhost:5051"