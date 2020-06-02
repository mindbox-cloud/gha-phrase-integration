using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class CreatePullRequestResponse
	{
		[DataMember(Name = "number")]
		public string Number { get; set; }

		[DataMember(Name = "head")]
		public HeadInfo Head { get; set; }
	}
}