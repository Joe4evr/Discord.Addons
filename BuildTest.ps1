Class ProjectRef
{
    [string] $ProjectLocation;
    [string] $TestsLocation;
    ProjectRef([string] $project, [string] $tests)
    {
        $this.ProjectLocation = $project;
        $this.TestsLocation = $tests;
    }

    BuildAndTest([string] $name)
    {
        Write-Host "Restore pakcages for project '"$name"'";
        dotnet restore $this.ProjectLocation;
        if ($LastExitCode -ne 0)
        {
            Write-Host "Failed to find/restore project";
            return;
        }

        if (-not [String]::IsNullOrEmpty($this.TestsLocation))
        {
            Write-Host "Testing project '"$name"'";
            dotnet test $this.TestsLocation;
            if ($LastExitCode -ne 0)
            {
                Write-Host "Package not published because of failed tests";
                return;
            }
        }
        
        Write-Host "Packing project '"$name"'";
        dotnet pack $this.ProjectLocation -c Release -o C:\nugetpacks;
    }
}

$projects = @{
    "MpGame" = [ProjectRef]::new("src\Discord.Addons.MpGame\Discord.Addons.MpGame.csproj", "test\MpGame.Tests\MpGame.Tests.csproj");
    "SimplePermissions" = [ProjectRef]::new("src\Discord.Addons.SimplePermissions\Discord.Addons.SimplePermissions.csproj", [String]::Empty);
}

if ($args[0] -eq "all")
{
    foreach ($project in $projects.Values)
    {
        $project.BuildAndTest();
    }
}
else
{
    if ($args.Length -gt 0)
    {
        foreach ($arg in $args)
        {
            if ($projects.ContainsKey($arg))
            {
                $projects[$arg].BuildAndTest($arg);
            }
        }
    }
    else
    {
        Write-Host "No args given.";
    }
}
pause