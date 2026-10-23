namespace ElementTemplates
{
	using Skyline.DataMiner.Core.GQI.Extensions;

	/// <summary>Returns all configured trend rules, including disabled and repeated rules.</summary>
	[GQIMetaData(Name = "Element trend template configuration")]
	public sealed class TrendTemplateSource : IGQIDataSource, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private readonly TemplateQuery query;

		/// <summary>Creates a query in the current user's DMS context.</summary>
		public TrendTemplateSource(IGQIDMSInterface dms, IGQIUser user, IGQILogger logger)
		{
			query = new TemplateQuery(TemplateKind.Trend, new TemplateReader(dms, user), logger);
		}

		/// <inheritdoc/>
		public GQIColumn[] GetColumns() => query.GetColumns();

		/// <inheritdoc/>
		public GQIArgument[] GetInputArguments() => query.GetInputArguments();

		/// <inheritdoc/>
		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args) => query.ProcessArguments(args);

		/// <inheritdoc/>
		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args) => query.PrepareFetch();

		/// <inheritdoc/>
		public GQIPage GetNextPage(GetNextPageInputArgs args) => query.GetNextPage();
	}
}
