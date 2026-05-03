param(
    [string]$buildPath = "bin/x64/Debug",
    [string]$jsonFile = "repo.json"
)

$version = Get-Date -Format "yyyy.MM.dd.HHmm"
$zipName = "ServiceCarePackage.zip"
$zipPath = ".\$zipName"

# Update csproj version
$csprojPath = ".\ServiceCarePackage.csproj"

[xml]$csproj = Get-Content $csprojPath

# Try to find existing Version node
function Set-CsprojProperty($name, $value) {
    $node = $csproj.SelectSingleNode("//Project/PropertyGroup/$name")

    if ($node -ne $null) {
        $node.InnerText = $value
    } else {
        $propertyGroup = $csproj.SelectSingleNode("//Project/PropertyGroup")
        if ($propertyGroup -eq $null) {
            $propertyGroup = $csproj.CreateElement("PropertyGroup")
            $csproj.Project.AppendChild($propertyGroup) | Out-Null
        }

        $newNode = $csproj.CreateElement($name)
        $newNode.InnerText = $value
        $propertyGroup.AppendChild($newNode) | Out-Null
    }
}

Set-CsprojProperty "Version" $version
Set-CsprojProperty "AssemblyVersion" $version
Set-CsprojProperty "FileVersion" $version
Set-CsprojProperty "InformationalVersion" $version
Write-Host "✅ Updated csproj version to $version"

# Build solution/project
dotnet clean $csprojPath -c Debug -p:Platform=x64
dotnet build $csprojPath -c Debug -p:Platform=x64 --no-incremental

if ($LASTEXITCODE -ne 0) {
    throw "Build failed. Stopping release."
}

Write-Host "✅ Build completed"


# Compress build folder
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

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
