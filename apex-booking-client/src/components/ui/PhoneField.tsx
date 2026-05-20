import { useEffect, useRef, useState } from "react";
import { COUNTRY_CODES, parsePhoneValue } from "../../utils/phoneUtils";

interface PhoneFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  disabled?: boolean;
  error?: string;
}

export const PhoneField: React.FC<PhoneFieldProps> = ({
  label,
  value,
  onChange,
  required = false,
  disabled = false,
  error,
}) => {
  const [countryCode, setCountryCode] = useState("+1");
  const [phoneNumber, setPhoneNumber] = useState("");
  const lastProcessedValue = useRef("");

  const baseInput = "form-control bg-white border rounded-3 px-3 py-2 shadow-none";

  useEffect(() => {
    if (value !== lastProcessedValue.current) {
      lastProcessedValue.current = value;
      if (!value) {
        setPhoneNumber("");
        return;
      }
      const { countryCode: parsedCode, phoneNumber: parsedNumber } = parsePhoneValue(value);
      setCountryCode(parsedCode);
      setPhoneNumber(parsedNumber);
    }
  }, [value]);

  const handlePhoneChange = (newCountryCode: string, newPhoneNumber: string) => {
    const combinedString = `${newCountryCode}${newPhoneNumber}`;
    lastProcessedValue.current = combinedString;
    setCountryCode(newCountryCode);
    setPhoneNumber(newPhoneNumber);
    onChange(combinedString);
  };

  return (
    <div>
      <label className="form-label fw-semibold small mb-1">
        {label}
        {required && <span className="text-danger ms-1">*</span>}
      </label>
      <div className="d-flex gap-2">
        <select
          className="form-control bg-white border rounded-3 px-2 py-2 phone-country-select"
          value={countryCode}
          disabled={disabled}
          onChange={(e) => handlePhoneChange(e.target.value, phoneNumber)}
        >
          {COUNTRY_CODES.map((code) => (
            <option key={code.value} value={code.value}>
              {code.label}
            </option>
          ))}
        </select>
        <input
          className={`${baseInput} flex-grow-1`}
          type="tel"
          placeholder="Phone number"
          value={phoneNumber}
          disabled={disabled}
          onChange={(e) => handlePhoneChange(countryCode, e.target.value)}
        />
      </div>
      {error && (
        <div className="text-danger small mt-1">{error}</div>
      )}
    </div>
  );
};
