namespace ElementTemplates.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.RepresentationModel;

[TestClass]
public sealed class CatalogContractTests
{
	[TestMethod]
	public void CatalogIdentityMatchesVisualStudioPublishProfileAndDocumentation()
	{
		var directory = AppDomain.CurrentDomain.BaseDirectory;
		var yaml = new YamlStream();
		using (var reader = File.OpenText(Path.Combine(directory, "CatalogInformation", "manifest.yml")))
		{
			yaml.Load(reader);
		}

		var manifest = (YamlMappingNode)yaml.Documents.Single().RootNode;
		var id = Guid.Parse(((YamlScalarNode)manifest.Children[new YamlScalarNode("id")]).Value!);
		var title = ((YamlScalarNode)manifest.Children[new YamlScalarNode("title")]).Value!;
		Assert.AreEqual("Ad Hoc Data Source", ((YamlScalarNode)manifest.Children[new YamlScalarNode("type")]).Value);
		var profile = XDocument.Load(Path.Combine(directory, "Contract", "UploadToDataMinerCatalog.pubxml"));
		Assert.AreEqual("https://catalog.dataminer.services/details/" + id, profile.Descendants("PublishDir").Single().Value);
		var readme = File.ReadAllText(Path.Combine(directory, "CatalogInformation", "README.md"));
		StringAssert.StartsWith(readme, "# " + title);
		Assert.DoesNotContain("wip.png", readme);
	}
}
