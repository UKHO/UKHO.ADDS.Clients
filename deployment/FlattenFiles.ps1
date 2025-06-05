$subFolders = Get-ChildItem -Path "$env:BUILD_SOURCESDIRECTORY" -Directory
foreach ($folder in $subFolders) {
    $files = Get-ChildItem -Path $folder.FullName -File
    Write-Host "Moving files from $($folder.FullName) to root directory"
    foreach ($file in $files) {
        $destination = Join-Path -Path "$env:BUILD_SOURCESDIRECTORY" -ChildPath $file.Name
        Move-Item -Path $file.FullName -Destination $destination -Force
    }
}