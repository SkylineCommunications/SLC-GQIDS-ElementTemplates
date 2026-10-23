namespace ElementTemplates.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using Skyline.DataMiner.Core.GQI.Extensions;
using YamlDotNet.RepresentationModel;

[TestClass]
public sealed class AssistantContractTests
{
	[TestMethod]
	public void ToolFilesMatchCompiledIdentitiesArgumentsAndEveryColumn()
	{
		var ns = XNamespace.Get("http://www.skyline.be/automation");
		var script = XDocument.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Contract", "ElementTemplates.xml"));
		var files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assistant"), "*.md");
		Assert.HasCount(2, files);
		foreach (var file in files)
		{
			var text = File.ReadAllText(file).Replace("\r\n", "\n");
			Assert.IsTrue(text.StartsWith("---\n", StringComparison.Ordinal));
			var yamlEnd = text.IndexOf("\n---", 4, StringComparison.Ordinal);
			Assert.IsGreaterThan(0, yamlEnd);
			var yaml = new YamlStream();
			yaml.Load(new StringReader(text.Substring(4, yamlEnd - 4)));
			var document = (YamlMappingNode)yaml.Documents.Single().RootNode;
			var name = Scalar(document, "name");
			Assert.IsTrue(Regex.IsMatch(name, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"));
			Assert.IsLessThanOrEqualTo(128, name.Length);
			Assert.IsLessThanOrEqualTo(1024, Scalar(document, "description").Length);
			Assert.IsLessThanOrEqualTo(8192, text.Length - yamlEnd);
			var identity = JObject.Parse(Scalar(document, "dataSource"));
			Assert.AreEqual(typeof(IGQIDataSource).FullName, (string)identity["DataSourceInterface"]!);
			Assert.AreEqual(script.Root!.Element(ns + "Name")!.Value, (string)identity["ScriptName"]!);
			Assert.AreEqual(script.Descendants(ns + "Param").Single(element => (string?)element.Attribute("type") == "libraryName").Value,
				(string)identity["LibraryName"]!);
			var type = typeof(AlarmTemplateSource).Assembly.GetType((string)identity["TypeFullName"]!, true)!;
			Assert.AreEqual(type.Name, (string)identity["TypeName"]!);
			Assert.IsTrue(typeof(IGQIDataSource).IsAssignableFrom(type));
			var source = Activator.CreateInstance(type, Mock.Of<IGQIDMSInterface>(), Mock.Of<IGQIUser>(), Mock.Of<IGQILogger>())!;
			var columns = ((IGQIDataSource)source).GetColumns();
			var toolColumns = ((YamlSequenceNode)document.Children[new YamlScalarNode("columns")]).Children.Cast<YamlMappingNode>().ToArray();
			CollectionAssert.AreEqual(columns.Select(column => column.Name).ToArray(), toolColumns.Select(column => Scalar(column, "name")).ToArray());
			for (var i = 0; i < columns.Length; i++)
			{
				var expectedType = columns[i] is GQIIntColumn ? "Int32" : columns[i] is GQIBooleanColumn ? "Boolean" : "String";
				Assert.AreEqual(expectedType, Scalar(toolColumns[i], "type"));
			}

			var arguments = ((IGQIInputArguments)source).GetInputArguments();
			var toolArguments = ((YamlSequenceNode)document.Children[new YamlScalarNode("inputArguments")]).Children.Cast<YamlMappingNode>().ToArray();
			CollectionAssert.AreEqual(arguments.Select(argument => argument.Name).ToArray(), toolArguments.Select(argument => Scalar(argument, "name")).ToArray());
			Assert.IsTrue(arguments.All(argument => argument.IsRequired && argument is GQIIntArgument));
			Assert.IsTrue(toolArguments.All(argument => Scalar(argument, "type") == "Int32" && int.Parse(Scalar(argument, "example")) > 0));
		}
	}

	private static string Scalar(YamlMappingNode node, string name) => ((YamlScalarNode)node.Children[new YamlScalarNode(name)]).Value!;
}
