using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class MergeRequest
	{
		[DataMember(Name = "commit_message")]
		public string CommitMessage { get;set; }

		[DataMember(Name = "sha")]
		public string Sha { get;set; }
	}
}