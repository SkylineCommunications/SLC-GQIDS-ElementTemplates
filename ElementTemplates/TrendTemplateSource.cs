namespace ElementTemplates
{
	using Skyline.DataMiner.Analytics.GenericInterface;

	/// <summary>Returns all configured trend rules, including disabled and repeated rules.</summary>
	[GQIMetaData(Name = "Element trend template configuration")]
	public sealed class TrendTemplateSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit, IGQIOnPrepareFetch
	{
		private readonly TemplateQuery query = new TemplateQuery(TemplateKind.Trend);

		/// <inheritdoc/>
		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			query.Initialize(new TemplateReader(args.DMS.SendMessages), args.Logger);
			return new OnInitOutputArgs();
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
