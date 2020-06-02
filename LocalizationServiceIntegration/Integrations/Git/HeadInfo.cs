using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class HeadInfo
	{
		[DataMember(Name = "sha")]
		public string Sha { get; set; }
	}
}