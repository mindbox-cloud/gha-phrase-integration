using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public enum PullRequestState
	{
		[DataMember(Name = "closed")]
		Closed,

		[DataMember(Name = "open")]
		Open,
	}
}