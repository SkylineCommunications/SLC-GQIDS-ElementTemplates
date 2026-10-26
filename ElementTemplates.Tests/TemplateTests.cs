namespace ElementTemplates.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Messages;

[TestClass]
public sealed class TemplateTests
{
	[TestMethod]
	public void Alarm_PreservesEveryRuleAndRawThresholdWithoutNormalizingZero()
	{
		var fixture = new Fixture();
		var rules = new[]
		{
			new GetAlarmTemplateResponseMessage { ID = 7, Filter = "port-*", Monitored = true, CH = "0", CL = "-0.01", Normal = "", Type = "ABSOLUTE" },
			new GetAlarmTemplateResponseMessage { ID = 7, Filter = "port-*", Monitored = false, CH = null, MaH = "3;4", DisabledIf = "1", Type = "NORMAL" },
			new GetAlarmTemplateResponseMessage { ID = 999, Filter = "unreported", Monitored = false, Info = "0", Hysteresis = "2", smartBaseLineEnable = 1 },
		};
		fixture.Alarms["alarms"] = fixture.Alarm("alarms", rules);

		var rows = fixture.Read(TemplateKind.Alarm);
		var actual = rows.Where(row => Value(row, "RecordKind").Equals("AlarmRule")).ToArray();

		Assert.HasCount(3, actual);
		Assert.AreEqual("0", Value(actual[0], "CH"));
		Assert.AreEqual("-0.01", Value(actual[0], "CL"));
		Assert.AreEqual("", Value(actual[0], "Normal"));
		Assert.IsNull(Value(actual[1], "CH"));
		Assert.IsFalse((bool)Value(actual[1], "Monitored"));
		Assert.AreEqual("3;4", Value(actual[1], "MaH"));
		Assert.AreEqual("1", Value(actual[1], "DisabledIf"));
		Assert.AreEqual("ns", Value(actual[0], "Units"));
		Assert.AreEqual(999, Value(actual[2], "ParameterID"));
		Assert.IsNull(Value(actual[2], "ParameterName"));
		Assert.AreEqual("0", Value(actual[2], "Info"));
		var raw = JObject.Parse((string)Value(actual[2], "RawConfiguration"));
		Assert.AreEqual(1, (int)raw["smartBaseLineEnable"]!);
		Assert.HasCount(rows.Count, rows.Select(row => row.Key).Distinct().ToArray());
		Assert.IsTrue(rows.All(row => Value(row, "EffectiveCoverage").Equals("NotEvaluated")));
	}

	[TestMethod]
	public void Alarm_AllThresholdColumnsRemainExactStrings()
	{
		var fixture = new Fixture();
		fixture.Alarms["alarms"] = fixture.Alarm("alarms", new GetAlarmTemplateResponseMessage
		{
			ID = 7, CH = "0", MaH = "9", MiH = "8", WaH = "7", Normal = "6", WaL = "5",
			MiL = "4", MaL = "3", CL = "-2", Info = "a;b", Hysteresis = "0.10", Type = "RELATIVE",
		});
		var row = fixture.Read(TemplateKind.Alarm).Single(item => Value(item, "RecordKind").Equals("AlarmRule"));
		var expected = new Dictionary<string, string>
		{
			["CH"] = "0", ["MaH"] = "9", ["MiH"] = "8", ["WaH"] = "7", ["Normal"] = "6",
			["WaL"] = "5", ["MiL"] = "4", ["MaL"] = "3", ["CL"] = "-2", ["Info"] = "a;b",
			["Hysteresis"] = "0.10", ["ThresholdType"] = "RELATIVE",
		};
		foreach (var pair in expected)
		{
			Assert.AreEqual(pair.Value, Value(row, pair.Key));
		}
	}

	[TestMethod]
	public void Alarm_GroupRetainsDisabledMembersRepeatedChildrenOrderAndConditions()
	{
		var fixture = new Fixture();
		fixture.Alarms["alarms"] = fixture.Alarm("alarms");
		fixture.Alarms["alarms"].Type = AlarmTemplateType.Group;
		fixture.Alarms["alarms"].GroupEntries = new[]
		{
			new AlarmTemplateGroupEntry { Name = "child", IsEnabled = false, IsScheduled = true },
			new AlarmTemplateGroupEntry { Name = "child", IsEnabled = true },
		};
		fixture.Alarms["child"] = fixture.Alarm("child", new GetAlarmTemplateResponseMessage { ID = 7, CH = "0", DisabledIf = "2" });
		fixture.Alarms["child"].Conditions = new[] { new AlarmTemplateCondition(2, "device disabled") };
		fixture.Alarms["child"].IsScheduleEnabled = true;

		var rows = fixture.Read(TemplateKind.Alarm);
		var entries = rows.Where(row => Value(row, "RecordKind").Equals("GroupEntry")).ToArray();
		var rules = rows.Where(row => Value(row, "RecordKind").Equals("AlarmRule")).ToArray();

		Assert.HasCount(2, entries);
		Assert.HasCount(2, rules);
		Assert.AreEqual("root/0", Value(rules[0], "TemplatePath"));
		Assert.AreEqual("root/1", Value(rules[1], "TemplatePath"));
		Assert.IsFalse((bool)JObject.Parse((string)Value(entries[0], "RawConfiguration"))["IsEnabled"]!);
		var childHeader = rows.Single(row => Value(row, "RecordKind").Equals("Template") && Value(row, "TemplatePath").Equals("root/0"));
		var raw = JObject.Parse((string)Value(childHeader, "RawConfiguration"));
		Assert.AreEqual("device disabled", (string)raw["Conditions"]![0]!["Name"]!);
		Assert.IsTrue((bool)raw["IsScheduleEnabled"]!);
		Assert.HasCount(rows.Count, rows.Select(row => row.Key).Distinct().ToArray());
	}

	[TestMethod]
	public void GroupProjectionIsNotConfusedWithOriginalMemberRules()
	{
		var fixture = new Fixture();
		fixture.Alarms["alarms"] = fixture.Alarm("alarms", new GetAlarmTemplateResponseMessage { ID = 7, CH = "100" });
		fixture.Alarms["alarms"].Type = AlarmTemplateType.Group;
		fixture.Alarms["alarms"].GroupEntries = new[] { new AlarmTemplateGroupEntry { Name = "member" } };
		fixture.Alarms["member"] = fixture.Alarm("member", new GetAlarmTemplateResponseMessage { ID = 7, CH = "200" });
		var rows = fixture.Read(TemplateKind.Alarm);
		var projection = rows.Single(row => Value(row, "RecordKind").Equals("GroupProjectionRule"));
		var original = rows.Single(row => Value(row, "RecordKind").Equals("AlarmRule"));
		Assert.AreEqual("UnresolvedProjection", Value(projection, "Status"));
		Assert.AreEqual("100", Value(projection, "CH"));
		Assert.AreEqual("member", Value(original, "TemplateName"));
		Assert.AreEqual("200", Value(original, "CH"));
	}

	[TestMethod]
	public void GroupMemberFailureDoesNotReturnPartialSuccess()
	{
		var fixture = new Fixture();
		fixture.Alarms["alarms"].Type = AlarmTemplateType.Group;
		fixture.Alarms["alarms"].GroupEntries = new[] { new AlarmTemplateGroupEntry { Name = "missing" } };
		var query = fixture.Query(TemplateKind.Alarm);
		query.SetTarget("12/34");
		Assert.ThrowsExactly<KeyNotFoundException>(() => query.GetNextPage());
		Assert.ThrowsExactly<InvalidOperationException>(() => query.GetNextPage());
	}

	[TestMethod]
	public void Trend_PreservesDisabledDuplicateAndFilteredRules()
	{
		var fixture = new Fixture();
		fixture.Trend.Parameters = new[]
		{
			new ParamTrendingInfo(7, "p*", true, false),
			new ParamTrendingInfo(7, "p*", false, true),
			new ParamTrendingInfo(8, "*", false, false) { DisableCentralDBAverageTrended = true, EnableAnomalyDetection = null },
		};

		var rows = fixture.Read(TemplateKind.Trend);
		var actual = rows.Skip(1).ToArray();
		Assert.HasCount(3, actual);
		Assert.IsTrue((bool)Value(actual[0], "RealTimeTrended", TemplateKind.Trend));
		Assert.IsFalse((bool)Value(actual[0], "AverageTrended", TemplateKind.Trend));
		Assert.IsTrue((bool)Value(actual[1], "AverageTrended", TemplateKind.Trend));
		Assert.IsFalse((bool)Value(actual[2], "RealTimeTrended", TemplateKind.Trend));
		Assert.AreEqual("p*", Value(actual[1], "Filter"));
		Assert.IsTrue((bool)JObject.Parse((string)Value(actual[2], "RawConfiguration"))["DisableCentralDBAverageTrended"]!);
	}

	[TestMethod]
	public void MissingAssignmentsAndLegitimatelyEmptyTemplatesHaveDifferentStates()
	{
		foreach (var kind in new[] { TemplateKind.Alarm, TemplateKind.Trend })
		{
			var fixture = new Fixture();
			fixture.Element.ProtocolTemplate = null;
			fixture.Element.Trending = "";
			var missing = fixture.Read(kind);
			Assert.HasCount(1, missing);
			Assert.AreEqual("NoAssignment", Value(missing[0], "Status"));
			Assert.AreEqual("Assignment", Value(missing[0], "RecordKind"));
			Assert.IsFalse(fixture.Requests.Any(request => request is GetProtocolMessage));

			fixture.Element.ProtocolTemplate = "alarms";
			fixture.Element.Trending = "trends";
			var empty = fixture.Read(kind);
			Assert.HasCount(1, empty);
			Assert.AreEqual("EmptyTemplate", Value(empty[0], "Status"));
			Assert.AreEqual("Template", Value(empty[0], "RecordKind"));
		}
	}

	[TestMethod]
	public void PagingFetchesInitialDataWithoutCallbacksAndResetsWithStableKeys()
	{
		var fixture = new Fixture();
		fixture.Alarms["alarms"] = fixture.Alarm("alarms", Enumerable.Range(0, 205).Select(i => new GetAlarmTemplateResponseMessage { ID = 7, CH = i.ToString() }).ToArray());
		var query = fixture.Query(TemplateKind.Alarm);
		query.SetTarget("12/34");
		var first = query.GetNextPage();
		var second = query.GetNextPage();
		var third = query.GetNextPage();
		Assert.HasCount(100, first.Rows);
		Assert.HasCount(100, second.Rows);
		Assert.HasCount(6, third.Rows);
		Assert.IsTrue(first.HasNextPage);
		Assert.IsTrue(second.HasNextPage);
		Assert.IsFalse(third.HasNextPage);
		Assert.HasCount(0, query.GetNextPage().Rows);
		var all = first.Rows.Concat(second.Rows).Concat(third.Rows).ToArray();
		Assert.HasCount(206, all.Select(row => row.Key).Distinct().ToArray());
		Assert.AreEqual(1, fixture.Requests.Count(request => request is GetAlarmTemplateMessage));
		query.PrepareFetch();
		CollectionAssert.AreEqual(first.Rows.Select(row => row.Key).ToArray(), query.GetNextPage().Rows.Select(row => row.Key).ToArray());
	}

	[TestMethod]
	public void InstancesAndChangedArgumentsCannotReuseAnotherElementsRows()
	{
		var first = new Fixture();
		var second = new Fixture();
		second.Element.DataMinerID = 56;
		second.Element.ElementID = 78;
		var one = first.Query(TemplateKind.Alarm);
		var two = second.Query(TemplateKind.Alarm);
		one.SetTarget("12/34");
		two.SetTarget("56/78");
		Assert.AreEqual(12, Value(one.GetNextPage().Rows[0], "dmaID"));
		Assert.AreEqual(56, Value(two.GetNextPage().Rows[0], "dmaID"));
		first.Element.ElementID = 35;
		one.SetTarget("12/35");
		Assert.AreEqual(35, Value(one.GetNextPage().Rows[0], "elementID"));
	}

	[TestMethod]
	public void UninitializedFetchFailsExplicitly()
	{
		var query = new Fixture().Query(TemplateKind.Alarm);
		Assert.ThrowsExactly<InvalidOperationException>(() => query.GetNextPage());
	}

	[TestMethod]
	public void DiscoveryAndArgumentProcessingNeedNoDmsAndFetchRequiresInitialization()
	{
		foreach (var kind in new[] { TemplateKind.Alarm, TemplateKind.Trend })
		{
			var query = new TemplateQuery(kind);
			Assert.IsNotEmpty(query.GetColumns());
			Assert.HasCount(1, query.GetInputArguments());
			query.SetTarget("12/34");
			query.PrepareFetch();
			Assert.ThrowsExactly<InvalidOperationException>(() => query.GetNextPage());
			var fixture = new Fixture();
			query.Initialize(new TemplateReader(fixture.SendMessages), fixture.Logger.Object);
			Assert.HasCount(0, fixture.Requests);
			Assert.HasCount(1, query.GetNextPage().Rows);
			Assert.IsInstanceOfType<GetElementByIDMessage>(fixture.Requests[0]);
			Assert.IsFalse(fixture.Requests.Any(request => request is GetInfoMessage));
		}
	}

	[TestMethod]
	[DataRow("1/1", 1, 1)]
	[DataRow("12/34", 12, 34)]
	[DataRow("2147483647/2147483647", int.MaxValue, int.MaxValue)]
	public void CombinedElementIdentifierSelectsTheExactElement(string target, int dmaId, int elementId)
	{
		foreach (var kind in new[] { TemplateKind.Alarm, TemplateKind.Trend })
		{
			var fixture = new Fixture();
			fixture.Element.DataMinerID = dmaId;
			fixture.Element.ElementID = elementId;
			var query = fixture.Query(kind);
			var argument = query.GetInputArguments().Single();
			Assert.AreEqual("elementID", argument.Name);
			Assert.IsInstanceOfType<GQIStringArgument>(argument);
			Assert.IsTrue(argument.IsRequired);
			query.SetTarget(target);
			var row = query.GetNextPage().Rows.Single();
			Assert.AreEqual(dmaId, Value(row, "dmaID", kind));
			Assert.AreEqual(elementId, Value(row, "elementID", kind));
		}
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow(" ")]
	[DataRow("12")]
	[DataRow("/34")]
	[DataRow("12/")]
	[DataRow("12/34/56")]
	[DataRow("12\\34")]
	[DataRow("dmaID/elementID")]
	[DataRow("0/34")]
	[DataRow("12/0")]
	[DataRow("-12/34")]
	[DataRow("12/-34")]
	[DataRow("+12/34")]
	[DataRow("12/+34")]
	[DataRow(" 12/34")]
	[DataRow("12/34 ")]
	[DataRow("12 / 34")]
	[DataRow("12.0/34")]
	[DataRow("2147483648/34")]
	[DataRow("12/2147483648")]
	public void InvalidCombinedIdentifiersFailBeforeAnyRead(string? target)
	{
		foreach (var kind in new[] { TemplateKind.Alarm, TemplateKind.Trend })
		{
			var fixture = new Fixture();
			var query = fixture.Query(kind);
			var error = Assert.ThrowsExactly<ArgumentException>(() => query.SetTarget(target!));
			StringAssert.Contains(error.Message, "dmaID/elementID");
			Assert.ThrowsExactly<InvalidOperationException>(() => query.GetNextPage());
			Assert.HasCount(0, fixture.Requests);
		}
	}

	[TestMethod]
	public void InvalidChangedIdentifierCannotReusePreviouslyFetchedRows()
	{
		foreach (var kind in new[] { TemplateKind.Alarm, TemplateKind.Trend })
		{
			var fixture = new Fixture();
			var query = fixture.Query(kind);
			query.SetTarget("12/34");
			Assert.HasCount(1, query.GetNextPage().Rows);
			var requestCount = fixture.Requests.Count;
			Assert.ThrowsExactly<ArgumentException>(() => query.SetTarget("invalid"));
			query.PrepareFetch();
			Assert.ThrowsExactly<InvalidOperationException>(() => query.GetNextPage());
			Assert.HasCount(requestCount, fixture.Requests);
			query.SetTarget("12/34");
			Assert.HasCount(1, query.GetNextPage().Rows);
		}
	}

	[TestMethod]
	public void ServerDenialAndUnexpectedElementFailBeforeTemplateReads()
	{
		var fixture = new Fixture { Failure = new UnauthorizedAccessException("Server denied the caller access.") };
		Assert.ThrowsExactly<UnauthorizedAccessException>(() => fixture.Read(TemplateKind.Alarm));
		Assert.HasCount(1, fixture.Requests);
		Assert.IsInstanceOfType<GetElementByIDMessage>(fixture.Requests[0]);
		fixture.Failure = null;
		fixture.Element.ElementID = 999;
		Assert.ThrowsExactly<InvalidOperationException>(() => fixture.Read(TemplateKind.Alarm));
		Assert.IsFalse(fixture.Requests.Any(request => request is GetAlarmTemplateMessage));
	}

	[TestMethod]
	public void RetrievalFailureNeverBecomesAnEmptySuccessfulPageAndIsLogged()
	{
		var fixture = new Fixture();
		fixture.Failure = new TimeoutException("test transport timeout");
		var query = fixture.Query(TemplateKind.Alarm);
		query.SetTarget("12/34");
		Assert.ThrowsExactly<TimeoutException>(() => query.GetNextPage());
		Assert.ThrowsExactly<InvalidOperationException>(() => query.GetNextPage());
		fixture.Logger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
		fixture.Failure = null;
		query.PrepareFetch();
		Assert.AreEqual("EmptyTemplate", Value(query.GetNextPage().Rows[0], "Status"));
	}

	[TestMethod]
	public void IncompleteWrongIdentityAndCyclicDefinitionsAreNotHealthyResults()
	{
		var fixture = new Fixture();
		fixture.Alarms["alarms"].TemplateParams = null!;
		Assert.ThrowsExactly<NotSupportedException>(() => fixture.Read(TemplateKind.Alarm));
		fixture.Alarms["alarms"] = fixture.Alarm("different");
		Assert.ThrowsExactly<NotSupportedException>(() => fixture.Read(TemplateKind.Alarm));
		fixture.Alarms["alarms"] = fixture.Alarm("alarms");
		fixture.Alarms["alarms"].Type = AlarmTemplateType.Group;
		fixture.Alarms["alarms"].GroupEntries = new[] { new AlarmTemplateGroupEntry { Name = "alarms" } };
		Assert.ThrowsExactly<NotSupportedException>(() => fixture.Read(TemplateKind.Alarm));
		fixture.Trend.Parameters = null!;
		Assert.ThrowsExactly<NotSupportedException>(() => fixture.Read(TemplateKind.Trend));
	}

	[TestMethod]
	public void UnsupportedResponseIsDistinctFromMissingAssignment()
	{
		var fixture = new Fixture { UnexpectedResponse = true };
		Assert.ThrowsExactly<NotSupportedException>(() => fixture.Read(TemplateKind.Alarm));
	}

	private static object Value(GQIRow row, string name, TemplateKind kind = TemplateKind.Alarm)
	{
		var names = TemplateColumns.CommonNames.Concat(kind == TemplateKind.Alarm ? TemplateColumns.AlarmNames : TemplateColumns.TrendNames).ToArray();
		return row.Cells[Array.IndexOf(names, name)].Value;
	}

	private sealed class Fixture
	{
		internal readonly Mock<IGQILogger> Logger = new();
		internal readonly List<DMSMessage> Requests = new();
		internal readonly Dictionary<string, AlarmTemplateEventMessage> Alarms = new();
		internal readonly ElementInfoEventMessage Element = new()
		{
			DataMinerID = 12, ElementID = 34, Name = "device", Protocol = "Generic",
			ProtocolVersion = "Production", ProtocolTemplate = "alarms", Trending = "trends",
		};

		internal readonly GetTrendingTemplateInfoResponseMessage Trend = new()
		{
			Name = "trends", Protocol = "Generic", Version = "1.0.0.0", Parameters = Array.Empty<ParamTrendingInfo>(),
		};

		internal Exception? Failure;
		internal bool UnexpectedResponse;

		internal Fixture()
		{
			Alarms["alarms"] = Alarm("alarms");
		}

		internal DMSMessage[] SendMessages(DMSMessage[] messages)
		{
			Requests.AddRange(messages);
			if (Failure != null)
			{
				throw Failure;
			}

			return UnexpectedResponse ? Array.Empty<DMSMessage>() : messages.Select(Respond).ToArray();
		}

		internal AlarmTemplateEventMessage Alarm(string name, params GetAlarmTemplateResponseMessage[] rules) =>
			new() { Name = name, Protocol = "Generic", Version = "1.0.0.0", TemplateParams = rules };

		internal IReadOnlyList<GQIRow> Read(TemplateKind kind) => new TemplateReader(SendMessages).Read(kind, 12, 34);

		internal TemplateQuery Query(TemplateKind kind) => new(kind, new TemplateReader(SendMessages), Logger.Object);

		private DMSMessage Respond(DMSMessage request)
		{
			switch (request)
			{
				case GetElementByIDMessage:
					return Element;
				case GetProtocolMessage:
					return new GetProtocolInfoResponseMessage
					{
						Name = "Generic", Version = "1.0.0.0",
						Parameters = new[] { new ParameterInfo { ID = 7, Name = "generic parameter", Units = "ns" } },
					};
				case GetAlarmTemplateMessage alarm:
					Assert.IsTrue(alarm.AsOneObject);
					Assert.AreEqual("1.0.0.0", alarm.Version);
					return Alarms[alarm.Template];
				case GetTrendingTemplateInfoMessage:
					return Trend;
				default:
					throw new AssertFailedException("Unexpected request: " + request.GetType().FullName);
			}
		}
	}
}
