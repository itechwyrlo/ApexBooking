namespace ApexBooking.WebApi.Dtos
{
    public class CreateStaffRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email {get;set;} = string.Empty;
        public string ContactNumber {get;set;} = string.Empty;
        public int Capacity { get; set; }
        public string? Description { get; set; }
    }
}