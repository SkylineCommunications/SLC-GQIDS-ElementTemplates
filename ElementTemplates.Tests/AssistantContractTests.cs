namespace ElementTemplates.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;
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
			Assert.IsTrue(typeof(IGQIOnInit).IsAssignableFrom(type));
			var source = Activator.CreateInstance(type)!;
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
			Assert.HasCount(1, arguments);
			Assert.AreEqual("elementID", arguments[0].Name);
			CollectionAssert.AreEqual(arguments.Select(argument => argument.Name).ToArray(), toolArguments.Select(argument => Scalar(argument, "name")).ToArray());
			Assert.IsTrue(arguments[0].IsRequired && arguments[0] is GQIStringArgument);
			Assert.AreEqual("String", Scalar(toolArguments[0], "type"));
			new TemplateQuery(TemplateKind.Alarm)
				.SetTarget(Scalar(toolArguments[0], "example"));
		}
	}

	private static string Scalar(YamlMappingNode node, string name) => ((YamlScalarNode)node.Children[new YamlScalarNode(name)]).Value!;

	[TestMethod]
	public void ToolsDistinguishConfiguredEnablementFromEffectiveCoverage()
	{
		var tools = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assistant");
		var alarm = File.ReadAllText(Path.Combine(tools, "element-alarm-template-configuration.md"));
		StringAssert.Contains(alarm, "only `Monitored = true`");
		StringAssert.Contains(alarm, "`Monitored = false` can be used but are currently not in use");
		StringAssert.Contains(alarm, "Null flags on headers");
		StringAssert.Contains(alarm, "NotSeparatelyExposedByApi");
		var trend = File.ReadAllText(Path.Combine(tools, "element-trend-template-configuration.md"));
		StringAssert.Contains(trend, "`RealTimeTrended = true` OR `AverageTrended = true`");
		StringAssert.Contains(trend, "false/false rows can be trended but");
		StringAssert.Contains(trend, "Null flags on headers");
		StringAssert.Contains(trend, "that a device reports the parameter or that any history exists");
	}
}
