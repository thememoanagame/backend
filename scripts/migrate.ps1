[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, HelpMessage="Migration name")]
    [Alias("n")][string]$Name,
    [Parameter(Mandatory=$false, HelpMessage="Is App Migration")]
    [switch][Alias("a")]$App,
    [Parameter(Mandatory=$false, HelpMessage="Verbose")]
    [switch][Alias("v")]$VBS
)

$src = (Get-Item "$PSScriptRoot\..\src").FullName

$AppStartupProject = "src/app/MemoAna"
$AppProject = "src/app/MemoAna.Infrastructure"
$AppMigrationsFolder = "Persistence/Migrations"
$StartupProject = "src/web/MemoAna.Backend"
$Project = "src/web/MemoAna.Backend.Infrastructure"
$MigrationsFolder = "Persistence/Migrations"

$ProjectRoot = (Get-Item "$RepoRoot\$StartupProject").FullName

Push-Location $ProjectRoot
try {
    Write-Host $PWD
    Write-Host "Adding migration '$Name'" -ForegroundColor Cyan
    
    if ($VBS) {
        if ($App) {
            dotnet ef migrations add $Name --startup-project $AppStartupProject --project $AppProject --framework net10.0-android --output-dir $AppMigrationsFolder --verbose
        }
        else {
            dotnet ef migrations add $Name --startup-project $StartupProject --project $Project --output-dir $MigrationsFolder --verbose
        }
    }
    else {
        if ($App) {
            dotnet ef migrations add $Name --startup-project $AppStartupProject --project $AppProject --framework net10.0-android --output-dir $AppMigrationsFolder 
        }
        else {
            dotnet ef migrations add $Name --startup-project $StartupProject --project $Project --output-dir $MigrationsFolder
        }
    }    

}
catch {
    Write-Error $_
}
finally {
    Pop-Location
}
