param(
    [string]$buildPath = "bin/x64/Debug",
    [string]$jsonFile = "repo.json"
)

$version = Get-Date -Format "yyyyMMdd.HHmmss"
$zipName = "ServiceCarePackage.zip"
$zipPath = ".\$zipName"

# Compress build folder
Compress-Archive -Path "$buildPath\*" -DestinationPath $zipPath -Force
Write-Host "✅ Zipped $buildPath into $zipPath"

# Create GitHub release and upload ZIP
gh release create $version $zipPath --title "$version Release" --notes "Automated release of version $version"
Write-Host "✅ GitHub release $version created"

# Construct download URL
$repoUrl = gh repo view --json nameWithOwner | ConvertFrom-Json
$downloadUrl = "https://github.com/$($repoUrl.nameWithOwner)/releases/download/$version/$zipName"

# Create JSON file
$json = Get-Content $jsonFile | ConvertFrom-Json
    $json[0].AssemblyVersion = $version
    $json[0].TestingAssemblyVersion = $version
    $json[0].DownloadLinkInstall = $downloadUrl
    $json[0].DownloadLinkUpdate = $downloadUrl
# $json | ConvertTo-Json -Depth 5 | Set-Content $jsonFile
$wrappedJson = "[" + ($json | ConvertTo-Json -Depth 5) + "]"
Set-Content $jsonFile $wrappedJson
Write-Host "✅ Updated $jsonFile with download URL"

# Commit JSON file
git add $jsonFile
git commit -m "Add release info for $version"
git push origin master
Write-Host "✅ Pushed $jsonFile to repository"
