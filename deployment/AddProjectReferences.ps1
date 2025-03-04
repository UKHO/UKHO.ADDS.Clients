param (
    [Parameter(Mandatory = $true)] [string] $CsProjPath,
    [Parameter(Mandatory = $true)] [string] $NuGetVersion,
    [Parameter(Mandatory = $true)] [string] $PackageSource,
    [Parameter(Mandatory = $true)] [string] $PackageName
)

Write-Host "Updating " $CsProjPath
Write-Host "Using version " $NuGetVersion
Write-Host "Package source " $PackageSource
Write-Host "Package name " $PackageName

$xmlContent = [xml](Get-Content $CsProjPath)

# This expects a csproj file containing a single PropertyGroup and two ItemGroup elements.
# We'll add a RestoreAdditionalProjectSources element to PropertyGroup to point to the folder containing the newly produced package.
# The first ItemGroup is expected to contain the PackageReference elements. We'll add an one for the newly built package in order to test it.
# The second ItemGroup is expected to contain the ProjectReference elements. We'll remove it as this is used for local development and we're now testing the separate package instead.

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
