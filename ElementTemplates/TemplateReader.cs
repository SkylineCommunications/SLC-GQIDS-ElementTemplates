namespace ElementTemplates
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	internal sealed class TemplateReader
	{
		private readonly Func<DMSMessage[], DMSMessage[]> sendMessages;

		internal TemplateReader(Func<DMSMessage[], DMSMessage[]> sendMessages)
		{
			this.sendMessages = sendMessages ?? throw new ArgumentNullException(nameof(sendMessages));
		}

		internal IReadOnlyList<GQIRow> Read(TemplateKind kind, int dmaId, int elementId)
		{
			var elementRequest = new GetElementByIDMessage(dmaId, elementId);
			var element = Request<ElementInfoEventMessage>(elementRequest);
			if (element.DataMinerID != dmaId || element.ElementID != elementId || element.IsDeleted)
			{
				throw new InvalidOperationException("The DMS did not resolve the exact requested element.");
			}

			var context = new TemplateContext(kind, element);
			if (String.IsNullOrEmpty(context.AssignedTemplate))
			{
				context.Add("Assignment", "NoAssignment", null, "root", null, null, null, null);
				return context.Rows;
			}

			var protocolRequest = new GetProtocolMessage(element.Protocol, element.ProtocolVersion);
			var protocol = Request<GetProtocolInfoResponseMessage>(protocolRequest);
			if (!String.Equals(protocol.Name, element.Protocol, StringComparison.Ordinal) || String.IsNullOrEmpty(protocol.Version))
			{
				throw new NotSupportedException("The connector identity could not be resolved.");
			}

			context.SetProtocol(protocol);
			if (kind == TemplateKind.Alarm)
			{
				ReadAlarm(context, context.AssignedTemplate, "root", new HashSet<string>(StringComparer.Ordinal));
			}
			else
			{
				ReadTrend(context);
			}

			return context.Rows;
		}

		private void ReadAlarm(TemplateContext context, string name, string path, HashSet<string> ancestors)
		{
			if (ancestors.Count >= 128)
			{
				throw new NotSupportedException("Alarm group nesting exceeds the supported inspection depth of 128.");
			}

			if (!ancestors.Add(name))
			{
				throw new NotSupportedException("A cyclic alarm template group cannot be inspected completely.");
			}

			var request = new GetAlarmTemplateMessage(context.Protocol, context.TemplateVersion, name) { AsOneObject = true };
			var template = Request<AlarmTemplateEventMessage>(request);
			context.VerifyTemplate(template.Protocol, template.Version, template.Name, name);
			if (template.IsDeleted || template.TemplateParams == null || template.GroupEntries == null)
			{
				throw new NotSupportedException("The alarm template definition is deleted or incomplete.");
			}

			if (template.Type != AlarmTemplateType.Template && template.Type != AlarmTemplateType.Group)
			{
				throw new NotSupportedException("The returned alarm template type is not supported.");
			}

			var header = new
			{
				template.Name, template.Protocol, template.Version, template.Type, template.Description,
				template.InterpreteVersion, template.Conditions, template.Schedule, template.IsScheduleEnabled,
				template.GroupEntries, template.IsUsedInGroup,
				InclusionState = "NotSeparatelyExposedByApi",
			};
			var empty = template.TemplateParams.Length == 0 && template.GroupEntries.Length == 0;
			context.Add("Template", empty ? "EmptyTemplate" : "Configured", name, path, null, null, header, null);
			for (var i = 0; i < template.TemplateParams.Length; i++)
			{
				var rule = template.TemplateParams[i] ?? throw new NotSupportedException("An alarm template contains a null rule.");
				context.Add(template.Type == AlarmTemplateType.Group ? "GroupProjectionRule" : "AlarmRule",
					template.Type == AlarmTemplateType.Group ? "UnresolvedProjection" : "Configured",
					name, path, i, rule.ID, rule, rule);
			}

			for (var i = 0; i < template.GroupEntries.Length; i++)
			{
				var entry = template.GroupEntries[i];
				if (entry == null || String.IsNullOrEmpty(entry.Name))
				{
					throw new NotSupportedException("An alarm group member has no identity.");
				}

				var childPath = path + "/" + i.ToString(CultureInfo.InvariantCulture);
				context.Add("GroupEntry", "Configured", name, childPath, i, null, entry, null);
				// Disabled and scheduled members still contain configured rules.
				ReadAlarm(context, entry.Name, childPath, ancestors);
			}

			ancestors.Remove(name);
		}

		private void ReadTrend(TemplateContext context)
		{
			var request = new GetTrendingTemplateInfoMessage(context.Protocol, context.TemplateVersion, context.AssignedTemplate);
			var template = Request<GetTrendingTemplateInfoResponseMessage>(request);
			context.VerifyTemplate(template.Protocol, template.Version, template.Name, context.AssignedTemplate);
			if (template.Parameters == null)
			{
				throw new NotSupportedException("The trend template has no inspectable parameter collection.");
			}

			context.Add("Template", template.Parameters.Length == 0 ? "EmptyTemplate" : "Configured", template.Name,
				"root", null, null, new { template.Name, template.Protocol, template.Version, template.Description }, null);
			for (var i = 0; i < template.Parameters.Length; i++)
			{
				var rule = template.Parameters[i] ?? throw new NotSupportedException("A trend template contains a null rule.");
				context.Add("TrendRule", "Configured", template.Name, "root", i, rule.ParameterID, rule, null, rule);
			}
		}

		private T Request<T>(ClientRequestMessage request) where T : DMSMessage
		{
			var response = sendMessages(new DMSMessage[] { request });
			if (response == null || response.Length != 1 || !(response[0] is T typed))
			{
				var actual = response == null ? "null" : String.Join(", ", response.Select(message => message?.GetType().Name ?? "null"));
				throw new NotSupportedException("Expected one " + typeof(T).Name + " response for " + request
					+ "; received " + (response == null ? "null" : response.Length.ToString(CultureInfo.InvariantCulture)) + " response(s): " + actual + ".");
			}

			return typed;
		}
	}

	internal sealed class TemplateContext
	{
		private readonly TemplateKind kind;
		private readonly ElementInfoEventMessage element;
		private readonly Dictionary<int, ParameterInfo> parameters = new Dictionary<int, ParameterInfo>();

		internal TemplateContext(TemplateKind kind, ElementInfoEventMessage element)
		{
			this.kind = kind;
			this.element = element;
			AssignedTemplate = kind == TemplateKind.Alarm ? element.ProtocolTemplate : element.Trending;
		}

		internal List<GQIRow> Rows { get; } = new List<GQIRow>();

		internal string AssignedTemplate { get; }

		internal string Protocol => element.Protocol;

		internal string TemplateVersion => element.ProtocolVersion;

		internal void SetProtocol(GetProtocolInfoResponseMessage protocol)
		{
			var all = protocol.AllParameters ?? throw new NotSupportedException("Connector parameter metadata is unavailable.");
			foreach (var parameter in all)
			{
				if (parameter == null)
				{
					throw new NotSupportedException("Connector parameter metadata contains a null entry.");
				}

				parameters[parameter.ID] = parameter;
			}
		}

		internal void VerifyTemplate(string protocol, string version, string actualName, string expectedName)
		{
			if (!String.Equals(protocol, Protocol, StringComparison.Ordinal) ||
				!String.Equals(version, TemplateVersion, StringComparison.Ordinal) ||
				!String.Equals(actualName, expectedName, StringComparison.Ordinal))
			{
				throw new NotSupportedException("The DMS returned a different template identity than requested.");
			}
		}

		internal void Add(string recordKind, string status, string template, string path, int? ordinal,
			int? parameterId, object raw, GetAlarmTemplateResponseMessage alarm, ParamTrendingInfo trend = null)
		{
			ParameterInfo parameter = null;
			if (parameterId.HasValue)
			{
				parameters.TryGetValue(parameterId.Value, out parameter);
			}

			var values = new List<object>
			{
				recordKind, status, element.DataMinerID, element.ElementID, element.Name, Protocol,
				element.ProtocolVersion, TemplateVersion, AssignedTemplate, template, path, ordinal, parameterId,
				parameter?.Name, parameter?.Units, alarm != null ? alarm.Filter : trend?.Filter,
				"NotEvaluated", raw == null ? null : Serialize(raw),
			};
			if (kind == TemplateKind.Alarm)
			{
				values.AddRange(new object[]
				{
					alarm == null ? (object)null : alarm.Monitored, alarm?.Type, alarm?.DisabledIf, alarm?.CH,
					alarm?.MaH, alarm?.MiH, alarm?.WaH, alarm?.Normal, alarm?.WaL, alarm?.MiL,
					alarm?.MaL, alarm?.CL, alarm?.Info, alarm?.Hysteresis,
				});
			}
			else
			{
				values.Add(trend == null ? (object)null : trend.RealTimeTrended);
				values.Add(trend == null ? (object)null : trend.AverageTrended);
			}

			var key = String.Join(":", element.DataMinerID.ToString(CultureInfo.InvariantCulture),
				element.ElementID.ToString(CultureInfo.InvariantCulture), kind.ToString(), path, recordKind,
				ordinal?.ToString(CultureInfo.InvariantCulture) ?? "header");
			Rows.Add(new GQIRow(key, values.Select(value => new GQICell { Value = value }).ToArray()));
		}

		private static string Serialize(object value)
		{
			using (var output = new StringWriter(CultureInfo.InvariantCulture))
			using (var writer = new JsonTextWriter(output))
			{
				JsonSerializer.Create(new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.None,
					NullValueHandling = NullValueHandling.Include,
					DefaultValueHandling = DefaultValueHandling.Include,
				}).Serialize(writer, value);
				return output.ToString();
			}
		}
	}
}
