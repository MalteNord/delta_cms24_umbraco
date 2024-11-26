using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cms24_delta_umbraco.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public bool Host { get; set; }

        [ForeignKey("Room")]
        public string RoomId { get; set; }
        public Room Room { get; set; }
        [Required]
        public string UserId { get; internal set; }
        
        public int Score { get; set; } = 0;
    }
}
