using SQLite;

namespace Overseer.Models
{
    public class EnforcedUser
    {
        [PrimaryKey]
        public string Id { get; set; }
        [MaxLength(60)]
        public string Nickname { get; set; }
        [MaxLength(60)]
        public string EnforcedNickname { get; set; }
    }
}
