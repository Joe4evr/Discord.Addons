@echo off 
dotnet pack Discord.Addons.MpGame\Discord.Addons.MpGame.csproj -c Release -o C:\nugetpacks
dotnet pack Discord.Addons.Preconditions\Discord.Addons.Preconditions.csproj -c Release -o C:\nugetpacks
dotnet pack Discord.Addons.SimplePermissions\Discord.Addons.SimplePermissions.csproj -c Release -o C:\nugetpacks
dotnet pack Discord.Addons.SimplePermissions.EFProvider\Discord.Addons.SimplePermissions.EFProvider.csproj -c Release -o C:\nugetpacks
dotnet pack Discord.Addons.SimplePermissions.JsonProvider\Discord.Addons.SimplePermissions.JsonProvider.csproj -c Release -o C:\nugetpacks
REM dotnet pack Discord.Addons.Trivia\Discord.Addons.Trivia.csproj -c Release -o C:\nugetpacks
pause