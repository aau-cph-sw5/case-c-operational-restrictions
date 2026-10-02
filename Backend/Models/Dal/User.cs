namespace Backend.Models;

public enum Role { Admin, Clinician, Scheduler }   // swap in your real roles

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public Role Role { get; set; }
}