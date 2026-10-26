namespace ElementTemplates.Tests;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using ElementTemplates.Package;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class AssistantInstallationTests
{
	private static readonly string[] ToolNames =
	{
		"element-alarm-template-configuration.md",
		"element-trend-template-configuration.md",
	};

	private string directory = null!;
	private string setup = null!;
	private string commonData = null!;
	private string destination = null!;

	[TestInitialize]
	public void CreateIsolatedDirectories()
	{
		directory = Path.Combine(Path.GetTempPath(), "ElementTemplates.Tests", Guid.NewGuid().ToString("N"));
		setup = Path.Combine(directory, "setup");
		commonData = Path.Combine(directory, "ProgramData");
		destination = Path.Combine(commonData, "Skyline Communications", "DataMiner Assistant",
			"Synced Documents", "Context", "Custom", "Adhoc");
		Directory.CreateDirectory(Path.Combine(setup, "Assistant"));
		foreach (var name in ToolNames)
		{
			File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assistant", name),
				Path.Combine(setup, "Assistant", name));
		}
	}

	[TestCleanup]
	public void RemoveIsolatedDirectories()
	{
		if (Directory.Exists(directory))
		{
			Directory.Delete(directory, true);
		}
	}

	[TestMethod]
	public void InstallAndUpgradeReplaceOnlyOwnedFiles()
	{
		Directory.CreateDirectory(destination);
		var unrelated = Path.Combine(destination, "unrelated.md");
		File.WriteAllText(unrelated, "Leave this tool alone.");
		AssistantToolInstaller.Install(setup, commonData);
		foreach (var name in ToolNames)
		{
			Assert.AreEqual(File.ReadAllText(Path.Combine(setup, "Assistant", name)),
				File.ReadAllText(Path.Combine(destination, name)));
			File.WriteAllText(Path.Combine(destination, name), "Old registration");
		}

		AssistantToolInstaller.Install(setup, commonData);
		foreach (var name in ToolNames)
		{
			Assert.AreEqual(File.ReadAllText(Path.Combine(setup, "Assistant", name)),
				File.ReadAllText(Path.Combine(destination, name)));
		}

		Assert.AreEqual("Leave this tool alone.", File.ReadAllText(unrelated));
		Assert.HasCount(3, Directory.GetFiles(destination));
	}

	[TestMethod]
	public void MissingDefinitionFailsBeforeWritingEitherRegistration()
	{
		File.Delete(Path.Combine(setup, "Assistant", ToolNames[1]));
		var error = Assert.ThrowsExactly<FileNotFoundException>(() => AssistantToolInstaller.Install(setup, commonData));
		Assert.AreEqual(ToolNames[1], error.FileName);
		Assert.IsFalse(Directory.Exists(destination));
	}

	[TestMethod]
	public void UnwritableDestinationIsNotSuccessfulInstallation()
	{
		Directory.CreateDirectory(destination);
		var blockedFile = Path.Combine(destination, ToolNames[0]);
		File.WriteAllText(blockedFile, "Preserve on failure");
		File.SetAttributes(blockedFile, FileAttributes.ReadOnly);
		try
		{
			Assert.ThrowsExactly<UnauthorizedAccessException>(() => AssistantToolInstaller.Install(setup, commonData));
			Assert.AreEqual("Preserve on failure", File.ReadAllText(blockedFile));
			Assert.IsFalse(File.Exists(Path.Combine(destination, ToolNames[1])));
		}
		finally
		{
			File.SetAttributes(blockedFile, FileAttributes.Normal);
		}
	}

	[TestMethod]
	public void SdkPackageContainsInstallerSourceAndCanonicalToolDefinitions()
	{
		var root = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
		while (root is not null && !File.Exists(Path.Combine(root.FullName, "global.json")))
		{
			root = root.Parent;
		}

		Assert.IsNotNull(root, "Tests must run from the repository build output.");
		var project = XDocument.Load(Path.Combine(root.FullName, "ElementTemplates.Package", "ElementTemplates.Package.csproj"));
		var version = project.Descendants("Version").Single().Value;
		var testProject = Path.Combine(root.FullName, "ElementTemplates.Tests") + Path.DirectorySeparatorChar;
		var output = AppDomain.CurrentDomain.BaseDirectory.Substring(testProject.Length);
		var packagePath = Path.Combine(root.FullName, "ElementTemplates.Package", output,
			"DataMinerBuild", "ElementTemplates.Package." + version + ".dmapp");
		using var package = ZipFile.OpenRead(packagePath);
		Assert.IsFalse(package.Entries.Any(entry => entry.FullName.Contains("ElementTemplates.Tests")));
		var info = XDocument.Parse(Read(package, "AppInfo.xml"));
		Assert.AreEqual("10.5.0", info.Root!.Element("MinDmaVersion")!.Value);
		Assert.AreEqual(version, info.Root.Element("Version")!.Value);
		var install = Read(package, @"Scripts\Install.xml");
		StringAssert.Contains(install, "AutomationEntryPointType.Types.InstallAppPackage");
		StringAssert.Contains(install, "installer.InstallDefaultContent()");
		StringAssert.Contains(install, "AssistantToolInstaller.Install");
		var source = Read(package, @"AppInstallContent\Scripts\ElementTemplates\Script_ElementTemplates.xml");
		StringAssert.Contains(source, "class AlarmTemplateSource");
		StringAssert.Contains(source, "class TrendTemplateSource");
		foreach (var name in ToolNames)
		{
			var packed = Read(package, @"AppInstallContent\SetupContent\Assistant\" + name);
			Assert.AreEqual(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assistant", name)), packed);
		}
	}

	private static string Read(ZipArchive package, string path)
	{
		var entry = package.Entries.Single(item => item.FullName.Replace('/', '\\') == path);
		using var reader = new StreamReader(entry.Open());
		return reader.ReadToEnd();
	}
}
