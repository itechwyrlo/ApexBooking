namespace ApexBooking.Core.Domain.Enums
{
    public enum SystemRole
    {
        Owner, //is the business owner.
        Admin, //an operational employee with broader permissions than Staff.
        Staff //performs the services.
    }
}