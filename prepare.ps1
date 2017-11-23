param (
  [string]$Source, # Rider SDK Packages folder, optional
  [string]$BuildCounter, # Set Rider plugin version to version from Packaging.Props + $BuildCounter, optional
  [string]$SinceBuild, # Set since-build in Rider plugin descriptor
  [string]$UntilBuild # Set until-build in Rider plugin descriptor
)

Set-StrictMode -Version Latest; $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
[System.IO.Directory]::SetCurrentDirectory($PSScriptRoot)

function SetPropertyValue($file, $name, $value)
{
  Write-Host "- ${file}: $name -> $value"

  $xml = New-Object xml
  $xml.PreserveWhitespace = $true
  $xml.Load($file)
  
  $node = $xml.SelectSingleNode("//$name")
  if($node -eq $null) { Write-Error "$name was not found in $file" }

  if ($node.InnerText -ne $value) {
    $node.InnerText = $value
    $xml.Save($file)
  }
}

function SetIdeaVersion($file, $since, $until)
{
  if ($since -or $until) {
    Write-Host "- ${file}: since-build -> $since, until-build -> $until"

    $xml = New-Object xml
    $xml.PreserveWhitespace = $true
    $xml.Load($file)
  
    $node = $xml.SelectSingleNode("//idea-version")
    if($node -eq $null) { Write-Error "idea-build was not found in $file" }

    if ($since) {
      $node.SetAttribute("since-build", $since)
    }

    if ($until) {
      $node.SetAttribute("until-build", $until)
    }

    $xml.Save($file)
  }
}

function SetPluginVersion($file, $version)
{
  Write-Host "- ${file}: version -> $version"

  $xml = New-Object xml
  $xml.PreserveWhitespace = $true
  $xml.Load($file)
  
  $node = $xml.SelectSingleNode("//version")
  if($node -eq $null) { Write-Error "//version was not found in $file" }

  $node.InnerText = $version
  $xml.Save($file)
}

function SetNuspecVersion($file, $version)
{
  Write-Host "- ${file}: version -> $version"

  $xml = New-Object xml
  $xml.PreserveWhitespace = $true
  $xml.Load($file)

  $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
  $ns.AddNamespace("ns", $xml.DocumentElement.NamespaceURI)
  
  $node = $xml.SelectSingleNode("//ns:version", $ns)
  if($node -eq $null) { Write-Error "//version was not found in $file" }

  $node.InnerText = $version
  $xml.Save($file)
}

function GetPackageVersionFromFolder($folder, $name) {
  Write-Host "Looking for package $name among items:"
  foreach ($file in Get-ChildItem $folder) {
    Write-Host $file
    $match = [regex]::Match($file.Name, "^" + [Regex]::Escape($name) + "\.((\d+\.)+\d+((\-eap|\-snapshot)\d+(d?)(internal)?)?)\.nupkg$",
        [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success) {
      return $match.Groups[1].Value
    }
  }

  Write-Error "Package $name was not found in folder $folder"
}

function SetRiderSDKVersions($sdkPackageVersion)
{
  Write-Host "Setting versions:"
  Write-Host "  JetBrains.Rider.SDK -> $sdkPackageVersion"
  Write-Host "  JetBrains.Rider.SDK.Tests -> $sdkPackageVersion"  

  SetPropertyValue  "ReSharper.FSharp/Directory.Build.props" "RiderSDKVersion" "[$sdkPackageVersion]"  
}

$baseVersion = [IO.File]::ReadAllText("VERSION.txt").Trim()
if ($BuildCounter) {
  $version = "$baseVersion.$BuildCounter"
} else {
  $version = $baseVersion
}
Write-Host "##teamcity[buildNumber '$version']"

# not currently used
# SetIdeaVersion -file "rider-fsharp/src/main/resources/META-INF/plugin.xml" -since $SinceBuild -until $UntilBuild
SetPluginVersion -file "rider-fsharp/src/main/resources/META-INF/plugin.xml" -version $version
SetNuspecVersion -file "ReSharper.FSharp/ReSharper.FSharp.nuspec" -version $version

if ($Source) {
  $sdkPackageVersion = GetPackageVersionFromFolder $Source "JetBrains.Rider.SDK"
  SetRiderSDKVersions -sdkPackageVersion $sdkPackageVersion
}

tools\nuget restore -Source $Source -Source https://www.nuget.org/api/v2/ -Source http://repo.labs.intellij.net/api/nuget/dotnet-build "ReSharper.FSharp/ReSharper.FSharp.sln"

Write-Host "Finishing..."
Exit 0