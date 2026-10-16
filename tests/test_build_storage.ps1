$ErrorActionPreference = 'Stop'
$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../unity_preview_template/Editor/StandaloneBuildStorage.cs'))
# Exercise the real storage implementation with a deterministic build API substitute.
$stubs = @'
namespace UnityEditor.Build.Reporting {
 public enum BuildResult { Succeeded, Failed }
 public class BuildReport { public BuildSummary summary = new BuildSummary(); }
 public class BuildSummary { public BuildResult result; }
}
namespace UnityEditor {
 public enum BuildTarget { StandaloneWindows64 }
 public enum BuildOptions { None }
 public static class BuildPipeline {
  public static bool Fail;
  public static string Content = "first";
  public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(string[] scenes, string path, BuildTarget target, BuildOptions options) {
   System.IO.File.WriteAllText(path, Content);
   System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), Content + ".asset"), Content);
   return new UnityEditor.Build.Reporting.BuildReport { summary = new UnityEditor.Build.Reporting.BuildSummary { result = Fail ? UnityEditor.Build.Reporting.BuildResult.Failed : UnityEditor.Build.Reporting.BuildResult.Succeeded } };
  }
 }
}
namespace UnityEngine { public static class Debug { public static void Log(object message) {} } }
'@
Add-Type -TypeDefinition ($source + $stubs)
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('catcafe-storage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $testRoot 'cat_cafe_sim_tool') -Force | Out-Null
$oldWorkspace = $env:CATCAFE_WORKSPACE_ROOT
$oldLegacy = $env:CATCAFE_STANDALONE_BUILD
function Assert-Storage($condition, $message) { if (-not $condition) { throw $message } }
try {
    $env:CATCAFE_WORKSPACE_ROOT = $testRoot
    $env:CATCAFE_STANDALONE_BUILD = $null
    $root = Join-Path $testRoot 'Logs/StandaloneBuild'
    $latest = Join-Path $root 'Latest/CatCafeStandalone.exe'
    [CatCafePreview.Editor.StandaloneBuildStorage]::Build(@('scene'))
    Assert-Storage ([IO.File]::ReadAllText($latest) -eq 'first') 'First build missing'
    [UnityEditor.BuildPipeline]::Content = 'second'
    [CatCafePreview.Editor.StandaloneBuildStorage]::Build(@('scene'))
    Assert-Storage ([IO.File]::ReadAllText($latest) -eq 'second') 'Replacement failed'
    Assert-Storage (-not (Test-Path (Join-Path $root 'Latest/first.asset'))) 'Stale asset retained'
    [UnityEditor.BuildPipeline]::Fail = $true
    $failed = $false
    try { [CatCafePreview.Editor.StandaloneBuildStorage]::Build(@('scene')) } catch { $failed = $true }
    Assert-Storage $failed 'Failure not reported'
    Assert-Storage ([IO.File]::ReadAllText($latest) -eq 'second') 'Failure destroyed last success'
    Assert-Storage (-not (Test-Path (Join-Path $root 'Pending'))) 'Failed output retained'
    [UnityEditor.BuildPipeline]::Fail = $false
    $env:CATCAFE_WORKSPACE_ROOT = $null
    $env:CATCAFE_STANDALONE_BUILD = Join-Path $testRoot 'Logs/SomeVerification/Build-new/CatCafeStandalone.exe'
    [CatCafePreview.Editor.StandaloneBuildStorage]::Build(@('scene'))
    Assert-Storage (-not (Test-Path (Join-Path $testRoot 'Logs/SomeVerification'))) 'Legacy path created another build'
    [IO.Directory]::Move((Join-Path $root 'Latest'), (Join-Path $root 'Previous'))
    [UnityEditor.BuildPipeline]::Fail = $true
    try { [CatCafePreview.Editor.StandaloneBuildStorage]::Build(@('scene')) } catch {}
    Assert-Storage (Test-Path $latest) 'Interrupted promotion not recovered'
    $lock = [IO.File]::Open((Join-Path $root 'build.lock'), 'Open', 'ReadWrite', 'None')
    try {
        $failed = $false
        try { [CatCafePreview.Editor.StandaloneBuildStorage]::Build(@('scene')) } catch { $failed = $true }
        Assert-Storage $failed 'Concurrent build lock ignored'
    } finally { $lock.Dispose() }
    Assert-Storage (@(Get-ChildItem -LiteralPath $root -Directory).Count -eq 1) 'Extra build directories remain'
    $env:CATCAFE_STANDALONE_BUILD = Join-Path $testRoot 'Custom/Build/CatCafeStandalone.exe'
    $failed = $false
    try { [CatCafePreview.Editor.StandaloneBuildStorage]::Build(@('scene')) } catch { $failed = $true }
    Assert-Storage $failed 'Unanchored legacy output accepted'
    $env:CATCAFE_STANDALONE_BUILD = $null
    [CatCafePreview.Editor.StandaloneBuildStorage]::Build(@('scene'))
    Assert-Storage ([IO.File]::ReadAllText($latest) -eq 'second') 'Verification-only call changed build'
    Write-Output 'PASS: replacement, stale assets, failed-build preservation/cleanup, legacy redirection, interrupted promotion, exclusive lock, single retained build'
} finally {
    $env:CATCAFE_WORKSPACE_ROOT = $oldWorkspace
    $env:CATCAFE_STANDALONE_BUILD = $oldLegacy
    $resolvedTestRoot = (Resolve-Path -LiteralPath $testRoot).Path
    if ((Split-Path $resolvedTestRoot -Leaf) -notlike 'catcafe-storage-*' -or (Split-Path $resolvedTestRoot -Parent) -ne ([IO.Path]::GetTempPath()).TrimEnd('\')) { throw 'Unexpected test cleanup root' }
    Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
}
