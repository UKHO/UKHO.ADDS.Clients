#param (
#    [Parameter(Mandatory = $true)] [string] $CsProjPath,
#    [Parameter(Mandatory = $true)] [string] $NuGetVersion,
#    [Parameter(Mandatory = $true)] [string] $PackageSource,
#    [Parameter(Mandatory = $true)] [string] $PackageName
#)

$CsProjPath = "C:\Code\UKHO.ADDS.Clients\test\UKHO.ADDS.Clients.FileShareService.ReadOnly.IntegrationTests\UKHO.ADDS.Clients.FileShareService.ReadOnly.IntegrationTests.csproj"
$NuGetVersion = "1.8.1253-alpha.4"
$PackageSource = "C:\Code\packages"
$PackageName = "UKHO.ADDS.Clients.FileShareService.ReadOnly"

Write-Host "Updating " $CsProjPath
Write-Host "Using version " $NuGetVersion
Write-Host "Package source " $PackageSource
Write-Host "Package name " $PackageName

$xmlContent = [xml](Get-Content $CsProjPath)

$propertyGroup = $xmlContent.Project.PropertyGroup

if ($propertyGroup -is [array]) {
    $propertyGroup = $propertyGroup[0]
}

$newRestoreSources = $xmlContent.CreateElement("RestoreAdditionalProjectSources", $xmlContent.DocumentElement.NamespaceURI)
$newRestoreSources.InnerText = $PackageSource
$propertyGroup.AppendChild($newRestoreSources) | Out-Null

$itemGroup = $xmlContent.Project.ItemGroup

if ($itemGroup -is [array]) {
    $itemGroup = $itemGroup[0]
}

$packageNode = $itemGroup.ProjectReference | Where-Object { $_.Include -Like "*UKHO.ADDS.Clients.FileShareService.ReadOnly*" }

if ($packageNode -eq $null) {
    throw "Error - unable to find " + $PackageName + " reference"
} else {
    $packageNode.SetAttribute("Include", $PackageName)
    $packageNode.SetAttribute("Version", $NuGetVersion)
}

$xmlContent.Save($CsProjPath)

Write-Host "Updated project file:"
Get-Content $CsProjPath
