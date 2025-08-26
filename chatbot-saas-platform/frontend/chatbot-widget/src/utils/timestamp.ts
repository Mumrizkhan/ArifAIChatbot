/**
 * Timestamp utility functions to ensure Redux serialization compatibility
 */

/**
 * Converts any timestamp value (Date, string, number) to ISO string format
 * This ensures Redux state remains serializable
 */
export function ensureISOString(timestamp: unknown): string {
  if (timestamp instanceof Date) {
    return timestamp.toISOString();
  }
  
  if (typeof timestamp === 'string') {
    // If it's already a string, validate it's a proper ISO string
    try {
      const date = new Date(timestamp);
      return date.toISOString();
    } catch {
      // If invalid date string, return current time
      return new Date().toISOString();
    }
  }
  
  if (typeof timestamp === 'number') {
    return new Date(timestamp).toISOString();
  }
  
  // Fallback to current time for any other type
  return new Date().toISOString();
}

/**
 * Sanitizes an object containing timestamp fields to ensure they're all ISO strings
 * Useful for API responses or SignalR events
 */
export function sanitizeTimestamps<T extends Record<string, unknown>>(obj: T): T {
  const sanitized = { ...obj } as Record<string, unknown>;
  
  // Common timestamp field names to check
  const timestampFields = ['timestamp', 'createdAt', 'updatedAt', 'endedAt', 'readAt', 'Timestamp', 'CreatedAt', 'UpdatedAt'];
  
  for (const field of timestampFields) {
    if (field in sanitized) {
      sanitized[field] = ensureISOString(sanitized[field]);
    }
  }
  
  return sanitized as T;
}
