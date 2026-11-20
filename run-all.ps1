# Starts the whole solution. The MVC app launches the API automatically and opens the browser.
Set-Location $PSScriptRoot
dotnet run --project src/RaceDay.Mvc --launch-profile http
