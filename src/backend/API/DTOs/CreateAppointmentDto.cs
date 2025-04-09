namespace API.Entities // Replace YourProjectName with your actual project namespace
{
    public class CreateAppointmentDto
    {
        public int ServiceId { get; set; }
        public DateTimeOffset Time { get; set; }
        public ClientDto? Client { get; set; }
    }
}