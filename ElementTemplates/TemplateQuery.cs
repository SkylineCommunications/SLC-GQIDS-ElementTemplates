namespace ElementTemplates
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Skyline.DataMiner.Core.GQI.Extensions;

	internal enum TemplateKind
	{
		Alarm,
		Trend,
	}

	internal sealed class TemplateQuery
	{
		internal const int PageSize = 100;
		private readonly object sync = new object();
		private readonly TemplateKind kind;
		private readonly TemplateReader reader;
		private readonly IGQILogger logger;
		private readonly GQIStringArgument elementArgument = new GQIStringArgument("elementID") { IsRequired = true };
		private int dmaId;
		private int elementId;
		private GQIRow[] rows;
		private int position;
		private bool failed;

		internal TemplateQuery(TemplateKind kind, TemplateReader reader, IGQILogger logger)
		{
			this.kind = kind;
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		internal GQIColumn[] GetColumns() => TemplateColumns.GetColumns(kind);

		internal GQIArgument[] GetInputArguments() => new GQIArgument[] { elementArgument };

		internal OnArgumentsProcessedOutputArgs ProcessArguments(OnArgumentsProcessedInputArgs args)
		{
			SetTarget(args.GetArgumentValue(elementArgument));
			return new OnArgumentsProcessedOutputArgs();
		}

		internal void SetTarget(string elementIdentifier)
		{
			lock (sync)
			{
				dmaId = 0;
				elementId = 0;
				Reset();

				var parts = elementIdentifier?.Split('/');
				if (parts == null || parts.Length != 2
					|| !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var dataMinerId)
					|| !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var targetElementId)
					|| dataMinerId <= 0 || targetElementId <= 0)
				{
					throw new ArgumentException("elementID must have the format dmaID/elementID, with two positive Int32 values (for example, 123/456).", nameof(elementIdentifier));
				}

				dmaId = dataMinerId;
				elementId = targetElementId;
			}
		}

		internal OnPrepareFetchOutputArgs PrepareFetch()
		{
			lock (sync)
			{
				Reset();
				return new OnPrepareFetchOutputArgs();
			}
		}

		internal GQIPage GetNextPage()
		{
			lock (sync)
			{
				if (dmaId <= 0 || elementId <= 0)
				{
					throw new InvalidOperationException("Process a valid elementID (dmaID/elementID) before fetching.");
				}

				if (failed)
				{
					throw new InvalidOperationException("Template inspection failed. Start a new fetch to retry.");
				}

				if (rows == null)
				{
					try
					{
						rows = reader.Read(kind, dmaId, elementId).ToArray();
					}
					catch (Exception ex)
					{
						failed = true;
						logger.Error(ex, "Element template inspection failed; no successful result was produced.");
						throw;
					}
				}

				var page = rows.Skip(position).Take(PageSize).ToArray();
				position += page.Length;
				return new GQIPage(page) { HasNextPage = position < rows.Length };
			}
		}

		private void Reset()
		{
			rows = null;
			position = 0;
			failed = false;
		}
	}

	internal static class TemplateColumns
	{
		internal static readonly string[] CommonNames =
		{
			"RecordKind", "Status", "dmaID", "elementID", "ElementName", "Protocol",
			"ElementProtocolVersion", "TemplateProtocolVersion", "AssignedTemplate", "TemplateName",
			"TemplatePath", "RowOrdinal", "ParameterID", "ParameterName", "Units", "Filter",
			"EffectiveCoverage", "RawConfiguration",
		};

		internal static readonly string[] AlarmNames =
		{
			"Monitored", "ThresholdType", "DisabledIf", "CH", "MaH", "MiH", "WaH", "Normal",
			"WaL", "MiL", "MaL", "CL", "Info", "Hysteresis",
		};

		internal static readonly string[] TrendNames = { "RealTimeTrended", "AverageTrended" };

		internal static GQIColumn[] GetColumns(TemplateKind kind)
		{
			var columns = new List<GQIColumn>();
			foreach (var name in CommonNames)
			{
				if (name == "dmaID" || name == "elementID" || name == "RowOrdinal" || name == "ParameterID")
				{
					columns.Add(new GQIIntColumn(name));
				}
				else
				{
					columns.Add(new GQIStringColumn(name));
				}
			}

			if (kind == TemplateKind.Alarm)
			{
				columns.AddRange(AlarmNames.Select(name => name == "Monitored" ? (GQIColumn)new GQIBooleanColumn(name) : new GQIStringColumn(name)));
			}
			else
			{
				columns.AddRange(TrendNames.Select(name => new GQIBooleanColumn(name)));
			}

			return columns.ToArray();
		}
	}
}
