using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.BookingEngine
{
    public interface ISlotGenerator
    {
        SlotGenerationResult GenerateAvailableSlots(SlotGenerationRequest request);
    }
}