param (
    [string] $OpenApiSpecPath,
    [string] $OutputDirectory,
    [string] $Language,
    [string] $GeneratedApiClassName,
    [string] $Namespace
)  

$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

[xml]$csproj = Get-Content $GeneratedApiClassName.csproj

$packageExist = $csproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq $packageName}

if(-Not $packageExist)
{
    dotnet add package Microsoft.Kiota.Bundle
}



$cmd = @(
"kiota generate",
"--openapi $OpenApiSpecPath ",
"--output $OutputDirectory ",
"--language $Language ",
"--class-name $GeneratedApiClassName ",
" --namespace-name $Namespace"
) -join " "

Write-Host "Running Kiota command:"
Write-Host "----------------------------------------"
Write-Host $cmd
Write-Host "----------------------------------------"
Invoke-Expression $cmd
