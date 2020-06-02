using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public enum StatusCheckState
	{
		[DataMember(Name = "pending")]
		Pending,

		[DataMember(Name = "success")]
		Success,

		[DataMember(Name = "failure")]
		Failure
	}
}