param (
    [Parameter(Mandatory = $true)] [string] $CsProjPath,
    [Parameter(Mandatory = $true)] [string] $NuGetVersion,
    [Parameter(Mandatory = $true)] [string] $PackageSource,
    [Parameter(Mandatory = $true)] [string] $PackageName
)

#$CsProjPath = "C:\Code\UKHO.ADDS.Clients\test\UKHO.ADDS.Clients.FileShareService.ReadOnly.IntegrationTests\UKHO.ADDS.Clients.FileShareService.ReadOnly.IntegrationTests.csproj"
#$NuGetVersion = "1.8.1253-alpha.4"
#$PackageSource = "C:\Code\packages"
#$PackageName = "UKHO.ADDS.Clients.FileShareService.ReadOnly"

Write-Host "Updating " $CsProjPath
Write-Host "Using version " $NuGetVersion
Write-Host "Package source " $PackageSource
Write-Host "Package name " $PackageName

$xmlContent = [xml](Get-Content $CsProjPath)

$propertyGroup = $xmlContent.Project.PropertyGroup

if ($propertyGroup -is [array]) {
    throw "Expected 1 PropertyGroup element in project file, found $($propertyGroup.Count)"
}

$newRestoreSources = $xmlContent.CreateElement("RestoreAdditionalProjectSources", $xmlContent.DocumentElement.NamespaceURI)
$newRestoreSources.InnerText = $PackageSource
$propertyGroup.AppendChild($newRestoreSources) | Out-Null

$itemGroup = $xmlContent.Project.ItemGroup

if ($itemGroup -is [array]) {

    if ($itemGroup.Count -ne 2) {
        throw "Expected 2 ItemGroup elements in project file, found $($itemGroup.Count)"
    }

    $xmlContent.Project.RemoveChild($itemGroup[1]) | Out-Null
} else {
    throw "Expected 2 ItemGroup elements in project file"
}

$newPackageReference = $xmlContent.CreateElement("PackageReference", $xmlContent.DocumentElement.NamespaceURI)
$newPackageReference.SetAttribute("Include", $PackageName)
$newPackageReference.SetAttribute("Version", $NuGetVersion)
$itemGroup[0].AppendChild($newPackageReference) | Out-Null

$xmlContent.Save($CsProjPath)

Write-Host "Updated project file:"
Get-Content $CsProjPath
