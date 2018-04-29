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
        Write-Host "Restoring pakcages for project '$name'";
        dotnet restore $this.ProjectLocation | Write-Host -ForegroundColor DarkGray;
        if ($LastExitCode -ne 0)
        {
            Write-Host "Failed to find/restore project" -ForegroundColor Red;
            return;
        }

        if (-not [String]::IsNullOrEmpty($this.TestsLocation))
        {
            Write-Host "Testing project '$name'";
            dotnet test $this.TestsLocation -c Release | Write-Host -ForegroundColor DarkGray;
            if ($LastExitCode -ne 0)
            {
                Write-Host "Package not published because of failed tests" -ForegroundColor Red;
                return;
            }
        }
        
        Write-Host "Packing project '$name'";
        dotnet pack $this.ProjectLocation -c Release -o C:\nugetpacks | Write-Host -ForegroundColor DarkGray;
        if ($LastExitCode -ne 0)
        {
            Write-Host "Packing failed" -ForegroundColor Red;
            return;
        }
    }
}

if ($args.Length -eq 0)
{
    Write-Host "No args given.";
    return;
}

$projects = @{
    "MpGame" = [ProjectRef]::new("src\Discord.Addons.MpGame\Discord.Addons.MpGame.csproj", "test\MpGame.Tests\MpGame.Tests.csproj");
    "SimplePermissions" = [ProjectRef]::new("src\Discord.Addons.SimplePermissions\Discord.Addons.SimplePermissions.csproj", [String]::Empty);
    "EFProvider" = [ProjectRef]::new("src\Discord.Addons.SimplePermissions.EFProvider\Discord.Addons.SimplePermissions.EFProvider.csproj", [String]::Empty);
};

if ($args.Length -eq 1 -and $args[0] -eq "all")
{
    foreach ($key in $projects.Keys)
    {
        $projects[$key].BuildAndTest($key);
    }
}
else
{
    foreach ($arg in $args)
    {
        if ($projects.ContainsKey($arg))
        {
            $projects[$arg].BuildAndTest($arg);
        }
    }
}

Write-Host "Building docs";
docfx "docs\docfx.json" | Write-Host -ForegroundColor DarkGray;
if ($LastExitCode -ne 0)
{
    Write-Host "Docs building failed" -ForegroundColor Red;
    return;
}
