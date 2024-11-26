using System.ComponentModel.DataAnnotations;

namespace cms24_delta_umbraco.Models{

    public class FormData
    {
        public string FormId { get; set; }
        [Required]
        public string Name { get; set; }
        [EmailAddress, Required]
        public string Email { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public bool DataConsent { get; set; }
    }
}