using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class StatusCheckInfo
	{
		[DataMember(Name = "state")]
		public StatusCheckState State { get; set; }
	}
}