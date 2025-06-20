param (
    [string] $OutputDirectory
)

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