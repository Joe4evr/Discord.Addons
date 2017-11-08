@echo off 
dotnet restore Discord.Addons.sln
dotnet pack src\Discord.Addons.MpGame\Discord.Addons.MpGame.csproj -c Release -o C:\nugetpacks
dotnet pack src\Discord.Addons.Preconditions\Discord.Addons.Preconditions.csproj -c Release -o C:\nugetpacks
dotnet pack src\Discord.Addons.SimplePermissions\Discord.Addons.SimplePermissions.csproj -c Release -o C:\nugetpacks
REM dotnet pack src\Discord.Addons.SimplePermissions.EFProvider\Discord.Addons.SimplePermissions.EFProvider.csproj -c Release -o C:\nugetpacks
REM dotnet pack src\Discord.Addons.SimplePermissions.JsonProvider\Discord.Addons.SimplePermissions.JsonProvider.csproj -c Release -o C:\nugetpacks
REM dotnet pack src\Discord.Addons.Trivia\Discord.Addons.Trivia.csproj -c Release -o C:\nugetpacks
pause