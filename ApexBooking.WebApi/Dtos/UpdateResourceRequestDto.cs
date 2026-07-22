namespace ApexBooking.WebApi.Dtos
{
    public sealed class UpdateResourceRequestDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email {get;set;} = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Capacity { get; set; }
    }
}