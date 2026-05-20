export const toRelativeTime = (createdAt: string): string => {
  const diff = Date.now() - new Date(createdAt).getTime();
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return 'Just now';
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days}d ago`;
  return new Date(createdAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

export const formatIsoDisplay = (iso: string): string => {
  if (!iso) return '';
  return new Date(iso).toLocaleString('en-US', { dateStyle: 'medium', timeStyle: 'short' });
};

export const formatTo12Hour = (time: string): string => {
  if (!time) return '';
  
  const [hoursStr, minutesStr] = time.split(':');
  let hours = parseInt(hoursStr, 10);
  const ampm = hours >= 12 ? 'PM' : 'AM';
  
  hours = hours % 12;
  hours = hours ? hours : 12; // the hour '0' should be '12'
  
  return `${hours}:${minutesStr} ${ampm}`;
};
