namespace ElementTemplates
{
	using Skyline.DataMiner.Core.GQI.Extensions;

	/// <summary>Returns configured alarm rules and their unmerged group provenance.</summary>
	[GQIMetaData(Name = "Element alarm template configuration")]
	public sealed class AlarmTemplateSource : IGQIDataSource, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private readonly TemplateQuery query;

		/// <summary>Creates a query in the current user's DMS context.</summary>
		public AlarmTemplateSource(IGQIDMSInterface dms, IGQIUser user, IGQILogger logger)
		{
			query = new TemplateQuery(TemplateKind.Alarm, new TemplateReader(dms, user), logger);
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
