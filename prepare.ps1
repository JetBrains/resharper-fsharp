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

function GetPackagesFromFolder($folder)
{
  $packages = @{}
  foreach ($file in Get-ChildItem $folder) {
    $match = [regex]::Match($file.Name, "^(.+?)\.((\d+\.)+\d+(\-eap\d+)?)\.nupkg$")
    if ($match.Success) {
      $name = $match.Groups[1].Value
      $version = $match.Groups[2].Value
      
      $packages.add($name, $version)
      Write-Host "- package $name $version"
    }
  }
  return $packages
}

function UpdatePackagesInPackagesConfig($file, $packages)
{
  Write-Host "* Updating ${file}"

  $xml = New-Object xml
  $xml.PreserveWhitespace = $true
  $xml.Load($file)
  
  foreach ($node in $xml.SelectNodes("//package")) {
    $version = $packages[$node.GetAttribute("id")]
    if ($version -ne $null) {
      $node.SetAttribute("version", $version)
    }
  }
  
  $xml.Save($file)
}

function ReplacePackageInString($str, $packages)
{
  foreach ($name in $packages.Keys) {
    $version = $packages[$name]
    $str = $str -ireplace ("packages\\" + [Regex]::Escape($name) + "\.((\d+\.)+\d+(\-eap\d+)?)\\"),"packages\${name}.${version}\"
  }
  return $str
}

function UpdatePackagesInProj($file, $packages)
{
  Write-Host "* Updating ${file}"

  $xml = New-Object xml
  $xml.PreserveWhitespace = $true
  $xml.Load($file)
  
  $ns = new-object Xml.XmlNamespaceManager $xml.NameTable
  $ns.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")
  
  foreach ($node in $xml.SelectNodes("//msb:HintPath", $ns)) {
    $node.InnerText = ReplacePackageInString $node.InnerText $packages
  }
  
  foreach ($node in $xml.SelectNodes("//msb:Reference", $ns)) {
    $inc = $node.GetAttribute("Include")
    if ($inc) {
      $inc = $inc -ireplace ", Version=[\d\.]+",""
      $inc = $inc -ireplace ", Culture=\w+",""
      $inc = $inc -ireplace ", PublicKeyToken=\w+",""
      $inc = $inc -ireplace ", processorArchitecture=\w+",""
      
      $node.SetAttribute("Include", $inc)
    }
  }
    
  foreach ($node in $xml.SelectNodes("//msb:Error", $ns)) {
    if ($node.GetAttribute("Condition")) { 
      $condition = ReplacePackageInString -str $node.GetAttribute("Condition") -packages $packages
      $node.SetAttribute("Condition", $condition)
    }
    if ($node.GetAttribute("Text")) { 
      $text = ReplacePackageInString $node.GetAttribute("Text") $packages
      $node.SetAttribute("Text", $text) 
    }
  }
  
  foreach ($node in $xml.SelectNodes("//msb:Import", $ns)) {
    if ($node.GetAttribute("Condition")) { 
      $condition = ReplacePackageInString -str $node.GetAttribute("Condition") -packages $packages
      $node.SetAttribute("Condition", $condition)
    }
    if ($node.GetAttribute("Project")) { 
      $project = ReplacePackageInString $node.GetAttribute("Project") $packages
      $node.SetAttribute("Project", $project)
    }
  }
  
  $xml.Save($file)
}

$baseVersion = [IO.File]::ReadAllText("VERSION.txt").Trim()
if ($BuildCounter) {
  $version = "$baseVersion.$BuildCounter"
} else {
  $version = $baseVersion
}
Write-Host "##teamcity[buildNumber '$version']"

SetIdeaVersion -file "rider-fsharp/src/main/resources/META-INF/plugin.xml" -since $SinceBuild -until $UntilBuild
SetPluginVersion -file "rider-fsharp/src/main/resources/META-INF/plugin.xml" -version $version
SetNuspecVersion -file "ReSharper.FSharp/ReSharper.FSharp.nuspec" -version $version

if ($Source) {
  $packages = GetPackagesFromFolder -folder $Source
  foreach ($config in Get-ChildItem -Path ReSharper.FSharp/src -Filter packages.config -Recurse) {
    UpdatePackagesInPackagesConfig $config.FullName $packages
  }

  foreach ($proj in Get-ChildItem -Path ReSharper.FSharp/src -Filter *.*proj -Recurse) {
    UpdatePackagesInProj $proj.FullName $packages
  }
}

Write-Host "##teamcity[progressMessage 'Restoring packages']"
if ($Source) {
  & tools\nuget restore -Source $Source -Source https://www.nuget.org/api/v2/ -Source http://repo.labs.intellij.net/api/nuget/dotnet-build ReSharper.FSharp/ReSharper.FSharp.sln
} else {
  & tools\nuget restore ReSharper.FSharp/ReSharper.FSharp.sln
}
if ($LastExitCode -ne 0) { throw "Exec: Unable to nuget restore: exit code $LastExitCode" }
