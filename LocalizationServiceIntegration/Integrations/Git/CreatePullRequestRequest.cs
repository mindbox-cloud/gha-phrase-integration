using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class CreatePullRequestRequest
	{
		[DataMember(Name = "title")]
		public string Title { get;set; }

		[DataMember(Name = "head")]
		public string Head { get;set; }

		[DataMember(Name = "base")]
		public string Base { get;set; }
	}
}