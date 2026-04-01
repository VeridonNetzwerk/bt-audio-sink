<#
.SYNOPSIS
  Publishes local wiki markdown files to the GitHub wiki repository.

.USAGE
  powershell -ExecutionPolicy Bypass -File scripts/publish-wiki.ps1
#>

param(
  [string]$WikiRepoUrl = "https://github.com/VeridonNetzwerk/bt-audio-sink.wiki.git",
  [string]$SourceWikiDir = "wiki",
  [string]$TempDir = ".tmp/wiki-publish"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SourceWikiDir)) {
  throw "Source wiki folder not found: $SourceWikiDir"
}

if (Test-Path $TempDir) {
  Remove-Item -Recurse -Force $TempDir
}

New-Item -ItemType Directory -Path $TempDir | Out-Null

Write-Host "Cloning wiki repository..."
git clone $WikiRepoUrl $TempDir

Write-Host "Copying wiki pages..."
Get-ChildItem $SourceWikiDir -File -Filter *.md | ForEach-Object {
  Copy-Item $_.FullName (Join-Path $TempDir $_.Name) -Force
}

Push-Location $TempDir

Write-Host "Committing wiki updates..."
git add .
if ((git status --porcelain) -ne $null -and (git status --porcelain).Length -gt 0) {
  git commit -m "docs(wiki): update wiki pages"
  git push origin master
  Write-Host "Wiki successfully published."
}
else {
  Write-Host "No wiki changes to publish."
}

Pop-Location
