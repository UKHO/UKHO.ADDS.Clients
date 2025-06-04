param (
    [string] $OpenApiSpecPath,
    [string] $OutputDirectory,
    [string] $Language,
    [string] $GeneratedApiClassName,
    [string] $Namespace
)  

$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

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
