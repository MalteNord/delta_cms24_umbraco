using System.ComponentModel.DataAnnotations;

namespace cms24_delta_umbraco.Models
{
    public class Room
    {
        [Key]
        public string RoomId { get; set; } 
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsEnded { get; set; }
        public ICollection<Player> Players { get; set; } = new List<Player>();
    }
}
