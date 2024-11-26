using System.ComponentModel.DataAnnotations;

namespace cms24_delta_umbraco.Models
{
	public class SubmitAnswerRequest
	{
		[Required]
		public string UserId { get; set; }
	}
}
