public class Announcement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; }
    public DateTime Date { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public User User { get; set; }
}
