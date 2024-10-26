namespace BusinessObjects.OtherObjects
{
	public class BenchmarkResult
	{
		public int DatasetSize { get; set; }
		public Statistics DatabaseStats { get; set; }
		public Statistics ElasticsearchStats { get; set; }
		public string QueryType { get; set; } = "Basic";
	}
}
