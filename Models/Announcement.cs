public class Announcement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; }
    public DateTime Date { get; set; }

    public User User { get; set; }
}
