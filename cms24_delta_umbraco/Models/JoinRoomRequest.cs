using System;

namespace cms24_delta_umbraco.Models;

public class JoinRoomRequest
{
        public string PlayerName { get; set; }
        public bool Host { get; set; }
        public string? UserId { get; set; }
}
