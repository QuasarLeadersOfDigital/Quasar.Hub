using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Nuke.Docker;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Docker.DockerTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	[Solution] readonly Solution Solution;
	[GitRepository] readonly GitRepository GitRepository;
	[GitVersion] readonly GitVersion GitVersion;

	[Parameter] readonly string DockerHubUrl;
	[Parameter] readonly string DockerHubUsername;
	[Parameter] readonly string DockerHubPassword;

	AbsolutePath SourceDirectory => RootDirectory / "src";

	AbsolutePath TestsDirectory => RootDirectory / "tests";

	AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

	Target Clean => _ => _
		.Before(Restore)
		.Executes(() =>
		{
			SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
			TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
			EnsureCleanDirectory(ArtifactsDirectory);
		});

	Target Restore => _ => _
		.Executes(() =>
		{
			DotNetRestore(s => s
				.SetProjectFile(Solution));
		});

	Target Compile => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			DotNetBuild(s => s
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration)
				.SetAssemblyVersion(GitVersion.AssemblySemVer)
				.SetFileVersion(GitVersion.AssemblySemFileVer)
				.SetInformationalVersion(GitVersion.InformationalVersion)
				.EnableNoRestore());
		});

	Target Test => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			DotNetTest(s => s
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration)
				.EnableNoRestore()
				.EnableNoBuild());
		});

	Target ReverseProxyBuid => _ => _
		.Executes(() =>
		{
			DockerBuild(s => s
				.SetWorkingDirectory(RootDirectory + "/reverseproxy")
				.SetFile("./Dockerfile")
				.SetTag("quasar/hub/reverseproxy")
				.SetPath("."));
		});
	
	Target ServiceBuild => _ => _
		.Executes(() =>
		{
			DockerBuild(s => s
				.SetWorkingDirectory(RootDirectory)
				.SetFile("./Dockerfile")
				.SetTag("quasar/hub")
				.SetPath("."));
		});

	Target Deploy => _ => _
		.DependsOn(ReverseProxyBuid, ServiceBuild)
		.Executes(() =>
		{
			DockerLogin(s => s
				.SetServer(DockerHubUrl)
				.SetUsername(DockerHubUsername)
				.SetPassword(DockerHubPassword));
			
			DockerTag(s => s
				.SetSourceImage("quasar/hub/reverseproxy:latest")
				.SetTargetImage($"{DockerHubUrl}/quasar/hub/reverseproxy:latest"));

			DockerTag(s => s
				.SetSourceImage("quasar/hub:latest")
				.SetTargetImage($"{DockerHubUrl}/quasar/hub:latest"));
			
			DockerPush(s => s
				.SetName($"{DockerHubUrl}/quasar/hub/reverseproxy:latest"));

			DockerPush(s => s
				.SetName($"{DockerHubUrl}/quasar/hub:latest"));
		});

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);
}
