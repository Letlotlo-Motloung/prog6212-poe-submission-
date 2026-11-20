#!/usr/bin/env bash
# Starts the whole solution. The MVC app launches the API automatically and opens the browser.
cd "$(dirname "$0")"
dotnet run --project src/RaceDay.Mvc --launch-profile http
