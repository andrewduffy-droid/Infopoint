#!/bin/bash
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
exec /opt/homebrew/opt/dotnet@8/bin/dotnet "$@"