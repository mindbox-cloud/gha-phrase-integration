using RestSharp;

namespace LocalizationServiceIntegration
{
	public static class RestRequestExtensions
	{
		public static RestRequest WithParameter(this RestRequest request, string name, string value)
		{
			request.Parameters.Add(new Parameter
			{
				Type = ParameterType.QueryString,
				Name = name,
				Value = value
			});

			return request;
		}
	}
}