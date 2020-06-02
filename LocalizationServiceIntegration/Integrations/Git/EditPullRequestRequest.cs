using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class EditPullRequestRequest
	{
		[DataMember(Name = "state")]
		public PullRequestState State { get;set; }
	}
}