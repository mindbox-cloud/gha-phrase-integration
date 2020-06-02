using System.Runtime.Serialization;

[DataContract]
public enum MergeableState
{
	[DataMember(Name = "blocked")]
	Blocked,

	[DataMember(Name = "dirty")]
	Dirty,

	[DataMember(Name = "clean")]
	Clean,

	[DataMember(Name = "unknown")]
	Unknown,

	[DataMember(Name = "unstable")]
	Unstable
}