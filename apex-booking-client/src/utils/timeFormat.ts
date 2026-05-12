/**
 * Converts a 24-hour time string (HH:mm) to a 12-hour format with AM/PM.
 * Example: "09:00" -> "9:00 AM", "15:30" -> "3:30 PM"
 */
export const formatTo12Hour = (time: string): string => {
  if (!time) return '';
  
  const [hoursStr, minutesStr] = time.split(':');
  let hours = parseInt(hoursStr, 10);
  const ampm = hours >= 12 ? 'PM' : 'AM';
  
  hours = hours % 12;
  hours = hours ? hours : 12; // the hour '0' should be '12'
  
  return `${hours}:${minutesStr} ${ampm}`;
};
