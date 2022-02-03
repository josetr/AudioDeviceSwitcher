param([Parameter(Position=0)]$cmd, $configuration=$env:CONFIGURATION, $platform=$env:PLATFORM, $ci=$False)

if (!$configuration) {
	$configuration = "Release"
}

if (!$platform) {
	$platform = "x64"
}

$root = $PSScriptRoot
$projectName = 'AudioDeviceSwitcher'
$package_dir = "$root/src/$projectName (Package)"
$certificatePath = "$root/$projectName.pfx"
$sign = (Test-Path $certificatePath)

function Create-Package {
	msbuild -v:m -nologo `
	-p:Configuration=$configuration `
	-p:Platform=$platform `
	-p:AppxPackageSigningEnabled=$sign `
	-p:AppxBundlePlatforms=$platform `
	-p:AppPublishSingleFile=true `
	-p:UapAppxPackageBuildMode=StoreUpload
}

function Install-Audio-Devices {
	$url = "https://download.vb-audio.com/Download_CABLE/VBCABLE_Driver_Pack43.zip"
	$installDir = "$root/VB-Cable"
	$zip = "$root/VB-Cable.zip"
	$instances = 2

	Set-Service -Name "Audiosrv" -StartupType Automatic
    Start-Service Audiosrv

	New-Item -ItemType Directory -Force -Path $installDir
	Invoke-WebRequest $url -OutFile $zip
	Expand-Archive -LiteralPath $zip -DestinationPath $installDir

	$cert = (Get-AuthenticodeSignature "$installDir/vbaudio_cable64_win7.sys").SignerCertificate
	$store = [System.Security.Cryptography.X509Certificates.X509Store]::new("TrustedPublisher", "LocalMachine")
	$store.Open("ReadWrite")
	$store.Add($cert)
	$store.Close()

	for ($i = 0; $i -lt $instances; $i++)
	{
		&"$root/util/devcon" install "$installDir/vbMmeCable64_win7.inf" VBAudioVACWDM
	}

	exit 0
}

function Set-Package-Version {
	[xml]$manifest = Get-Content "$package_dir/Package.appxmanifest"
	$manifest.Package.Identity.Version = "$env:NBGV_SimpleVersion.0"
	$manifest.save("$package_dir/Package.appxmanifest")
}

function Restore {
	msbuild -v:m -nologo -p:Configuration=$configuration -p:Platform=$platform -t:restore
}

function Test {
	msbuild test/AudioDeviceSwitcher.Tests.csproj -v:m -nologo -p:Configuration=$configuration -p:Platform=$platform
	dotnet test bin\**\AudioDeviceSwitcher.Tests.dll
}

function Create-Certificate {
	Set-Content -Path "${projectName}.txt" -Value "$env:CERTIFICATE"
	certutil -decode "${projectName}.txt" "${projectName}.pfx"
	Remove-Item "${projectName}.txt"
}

switch ($cmd)
{
	"restore" { Restore }
	"test" { Test }
	"install-audio-devices" { Install-Audio-Devices }
	"set-package-version" { Set-Package-Version }
	"create-certificate" { Create-Certificate }
	default { Create-Package }
}
