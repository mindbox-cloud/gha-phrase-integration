using System.Runtime.Serialization;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class LocaleInfo
	{
		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "id")]
		public string Id { get; set; }

		[DataMember(Name = "isReference")]
		public bool IsReference { get; set; }
	}
}