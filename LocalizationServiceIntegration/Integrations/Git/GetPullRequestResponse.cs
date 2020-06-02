using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class GetPullRequestResponse
	{
		[DataMember(Name = "state")]
		public PullRequestState State { get; set; }

		[DataMember(Name = "mergeable")]
		public bool? Mergeable { get;set; }

		[DataMember(Name = "mergeable_state")]
		public MergeableState MergeableState { get; set; }
	}
}