// Validation utilities matching backend validators
export const isValidEmail = (email: string): boolean => {
  // Basic email validation (matches FluentValidation's EmailAddress)
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

export const isValidE164Phone = (phone: string): boolean => {
  // E.164 format: +[1-9][0-9]{1,14} (optional +)
  const phoneRegex = /^\+?[1-9]\d{1,14}$/;
  return phoneRegex.test(phone);
};

export const isValidName = (name: string): boolean => {
  // Matches ^[a-zA-Z\s\-']*$
  const nameRegex = /^[a-zA-Z\s\-']*$/;
  return nameRegex.test(name);
};

export const isValidServiceOrResourceName = (name: string): boolean => {
  // Matches ^[a-zA-Z0-9\s\-()&.]*$
  const nameRegex = /^[a-zA-Z0-9\s\-()&.]*$/;
  return nameRegex.test(name);
};

export const hasNoScriptTags = (input: string): boolean => {
  if (!input) return true;
  const lowerInput = input.toLowerCase();
  return !lowerInput.includes("<script") && !lowerInput.includes("javascript:");
};

export interface ValidationRule {
  validate: (value: any, formValue?: any) => boolean;
  message: string;
  severity?: 'error' | 'warning';
}

export type ValidationErrors<T> = Partial<Record<keyof T, string>>;

export const validateField = (value: any, rules: ValidationRule[]): string | null => {
  for (const rule of rules) {
    if (!rule.validate(value)) {
      return rule.message;
    }
  }
  return null;
};

export const validateForm = <T>(formValue: T, schema: Partial<Record<keyof T, ValidationRule[]>>): ValidationErrors<T> => {
  const errors: ValidationErrors<T> = {};
  for (const key in schema) {
    const rules = schema[key];
    if (rules) {
      const error = validateField(formValue[key], rules);
      if (error) {
        errors[key] = error;
      }
    }
  }
  return errors;
};

// Predefined validation rule sets for common fields
export const validationRules: Record<string, ValidationRule[]> = {
  email: [
    { validate: (v: string) => v.trim().length > 0, message: "Email is required", severity: 'error' },
    { validate: isValidEmail, message: "Email must be a valid email address", severity: 'error' },
    { validate: (v: string) => v.length <= 256, message: "Email cannot exceed 256 characters", severity: 'error' },
    { validate: (v: string) => !v.toLowerCase().includes("javascript:"), message: "Email contains invalid characters", severity: 'error' },
  ],
  password: [
    { validate: (v: string) => v.trim().length > 0, message: "Password is required", severity: 'error' },
    { validate: (v: string) => v.length >= 8, message: "Password must be at least 8 characters", severity: 'error' },
    { validate: (v: string) => v.length <= 256, message: "Password cannot exceed 256 characters", severity: 'error' },
  ],
  registerPassword: [
    { validate: (v: string) => v.trim().length > 0, message: "Password is required", severity: 'error' },
    { validate: (v: string) => v.length >= 8, message: "Password must be at least 8 characters", severity: 'error' },
    { validate: (v: string) => v.length <= 256, message: "Password cannot exceed 256 characters", severity: 'error' },
  ],
  firstName: [
    { validate: (v: string) => v.trim().length > 0, message: "First name is required", severity: 'error' },
    { validate: (v: string) => v.length <= 100, message: "First name cannot exceed 100 characters", severity: 'error' },
    { validate: isValidName, message: "First name contains invalid characters", severity: 'error' },
  ],
  lastName: [
    { validate: (v: string) => v.trim().length > 0, message: "Last name is required", severity: 'error' },
    { validate: (v: string) => v.length <= 100, message: "Last name cannot exceed 100 characters", severity: 'error' },
    { validate: isValidName, message: "Last name contains invalid characters", severity: 'error' },
  ],
  organizationName: [
    { validate: (v: string) => v.trim().length > 0, message: "Organization name is required", severity: 'error' },
    { validate: (v: string) => v.length <= 256, message: "Organization name cannot exceed 256 characters", severity: 'error' },
  ],
  phone: [
    { validate: (v: string) => v.trim().length > 0, message: "Phone is required", severity: 'error' },
    { validate: isValidE164Phone, message: "Phone must be a valid E.164 format", severity: 'error' },
  ],
  country: [
    { validate: (v: string) => v.trim().length > 0, message: "Country is required", severity: 'error' },
    { validate: (v: string) => v.length <= 100, message: "Country cannot exceed 100 characters", severity: 'error' },
  ],
  serviceName: [
    { validate: (v: string) => v.trim().length > 0, message: "Service name is required", severity: 'error' },
    { validate: (v: string) => v.length <= 256, message: "Service name cannot exceed 256 characters", severity: 'error' },
    { validate: isValidServiceOrResourceName, message: "Service name contains invalid characters", severity: 'error' },
  ],
  serviceDescription: [
    { validate: (v: string) => !v || v.length <= 1000, message: "Description cannot exceed 1000 characters", severity: 'error' },
    { validate: hasNoScriptTags, message: "Description contains invalid characters", severity: 'error' },
  ],
  durationMinutes: [
    { validate: (v: number) => v > 0, message: "Duration must be greater than 0", severity: 'error' },
    { validate: (v: number) => v <= 480, message: "Duration cannot exceed 8 hours", severity: 'error' },
  ],
  price: [
    { validate: (v: number) => v >= 0, message: "Price cannot be negative", severity: 'error' },
    { validate: (v: number) => v <= 999999.99, message: "Price cannot exceed 999999.99", severity: 'error' },
  ],
  resourceName: [
    { validate: (v: string) => v.trim().length > 0, message: "Resource name is required", severity: 'error' },
    { validate: (v: string) => v.length <= 256, message: "Resource name cannot exceed 256 characters", severity: 'error' },
    { validate: isValidServiceOrResourceName, message: "Resource name contains invalid characters", severity: 'error' },
  ],
  resourceDescription: [
    { validate: (v: string) => !v || v.length <= 1000, message: "Description cannot exceed 1000 characters", severity: 'error' },
    { validate: hasNoScriptTags, message: "Description contains invalid characters", severity: 'error' },
  ],
  capacity: [
    { validate: (v: number) => v > 0, message: "Capacity must be greater than 0", severity: 'error' },
  ],
  customerNotes: [
    { validate: (v: string) => !v || v.length <= 1000, message: "Customer notes cannot exceed 1000 characters", severity: 'error' },
    { validate: hasNoScriptTags, message: "Customer notes contains invalid characters", severity: 'error' },
  ],
  cancelReason: [
    { validate: (v: string) => !v || v.length <= 500, message: "Reason cannot exceed 500 characters", severity: 'error' },
    { validate: hasNoScriptTags, message: "Reason contains invalid characters", severity: 'error' },
  ],
  token: [
    { validate: (v: string) => v.trim().length > 0, message: "Token is required", severity: 'error' },
    { validate: (v: string) => v.length <= 1000, message: "Token is too long", severity: 'error' },
  ],
};