Class ProjectRef
{
    [string] $ProjectLocation;
    [string] $TestsLocation;
    [string] $DocsLocation;
    ProjectRef([string] $project, [string] $tests, [string] $docs)
    {
        $this.ProjectLocation = $project;
        $this.TestsLocation = $tests;
        $this.DocsLocation = $docs;
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


        if (-not [String]::IsNullOrEmpty($this.DocsLocation))
        {
            Write-Host "Building docs";
            docfx "docs\docfx.json" | Write-Host -ForegroundColor DarkGray;
            if ($LastExitCode -ne 0)
            {
                Write-Host "Docs building failed" -ForegroundColor Red;
                return;
            }

            Copy-Item -Path "docs\_site\*" -Destination $this.DocsLocation -Recurse -Force;
        }
    }
}

$projects = @{
    "MpGame" = [ProjectRef]::new("src\Discord.Addons.MpGame\Discord.Addons.MpGame.csproj", "test\MpGame.Tests\MpGame.Tests.csproj", "docs\mpgame\");
    "SimplePermissions" = [ProjectRef]::new("src\Discord.Addons.SimplePermissions\Discord.Addons.SimplePermissions.csproj", [String]::Empty, [String]::Empty);
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
