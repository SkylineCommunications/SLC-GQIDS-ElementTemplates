namespace ElementTemplates
{
	using Skyline.DataMiner.Analytics.GenericInterface;

	/// <summary>Returns configured alarm rules and their unmerged group provenance.</summary>
	[GQIMetaData(Name = "Element alarm template configuration")]
	public sealed class AlarmTemplateSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit, IGQIOnPrepareFetch
	{
		private readonly TemplateQuery query = new TemplateQuery(TemplateKind.Alarm);

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
