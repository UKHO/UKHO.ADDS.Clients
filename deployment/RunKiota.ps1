param (
    [string] $OpenApiSpecPath,
    [string] $OutputDirectory,
    [string] $Language,
    [string] $GeneratedApiClassName,
    [string] $Namespace
)  

$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Ensure Kiota is installed
$packageName = "Microsoft.Kiota.Bundle"
Write-Host "Checking if Kiota is installed on $OutputDirectory.csproj..."
$csprojPath = Get-ChildItem -Path . -Recurse -Filter "*$OutputDirectory.csproj" | Select-Object -First 1
if ($null -eq $csprojPath) {
    Write-Error "Could not find $OutputDirectory.csproj"
    exit 1
}
[xml]$csproj = Get-Content $csprojPath.FullName

$packageExist = $csproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $packageName }

if (-Not $packageExist) {
    dotnet add $csprojPath.FullName package $packageName
}

$cmd = @(
    "kiota generate",
    "--openapi $OpenApiSpecPath",
    "--output $OutputDirectory",
    "--language $Language",
    "--class-name $GeneratedApiClassName",
    "--namespace-name $Namespace"
) -join " "

Write-Host "Running Kiota command:"
Write-Host "----------------------------------------"
Write-Host $cmd
Write-Host "----------------------------------------"
Invoke-Expression $cmd