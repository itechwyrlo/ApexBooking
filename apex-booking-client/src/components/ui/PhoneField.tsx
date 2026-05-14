import { useEffect, useRef, useState } from "react";

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
  
  // Track the last value sent to or received from the parent
  const lastProcessedValue = useRef("");

  const countryCodes = [
    { label: "+1 (US/CA)", value: "+1" },
    { label: "+44 (UK)", value: "+44" },
    { label: "+61 (AU)", value: "+61" },
    { label: "+86 (CN)", value: "+86" },
    { label: "+91 (IN)", value: "+91" },
    { label: "+81 (JP)", value: "+81" },
    { label: "+49 (DE)", value: "+49" },
    { label: "+33 (FR)", value: "+33" },
    { label: "+39 (IT)", value: "+39" },
    { label: "+34 (ES)", value: "+34" },
    { label: "+55 (BR)", value: "+55" },
    { label: "+52 (MX)", value: "+52" },
    { label: "+7 (RU)", value: "+7" },
    { label: "+82 (KR)", value: "+82" },
    { label: "+65 (SG)", value: "+65" },
    { label: "+64 (NZ)", value: "+64" },
    { label: "+63 (PH)", value: "+63" },
    { label: "+353 (IE)", value: "+353" },
    { label: "+31 (NL)", value: "+31" },
    { label: "+46 (SE)", value: "+46" },
    { label: "+47 (NO)", value: "+47" },
  ];

  useEffect(() => {
    // Only parse if the parent value has structurally changed from our last state
    if (value !== lastProcessedValue.current) {
      lastProcessedValue.current = value;

      if (!value) {
        setPhoneNumber("");
        return;
      }

      // Fixes potential regex failures if numbers miss a leading '+' character
      const cleanValue = value.startsWith("+") ? value : `+${value}`;

      // Sort country codes longest to shortest to avoid partial matches (like +1 matching +1242)
      const sortedCodes = [...countryCodes].sort((a, b) => b.value.length - a.value.length);
      const matchedCode = sortedCodes.find((c) => cleanValue.startsWith(c.value));

      if (matchedCode) {
        setCountryCode(matchedCode.value);
        setPhoneNumber(cleanValue.slice(matchedCode.value.length));
      } else {
        setCountryCode("+1");
        setPhoneNumber(value);
      }
    }
  }, [value]);

  const handlePhoneChange = (newCountryCode: string, newPhoneNumber: string) => {
    const combinedString = `${newCountryCode}${newPhoneNumber}`;
    
    // Update our reference first so the useEffect knows we typed this change
    lastProcessedValue.current = combinedString;
    
    setCountryCode(newCountryCode);
    setPhoneNumber(newPhoneNumber);
    onChange(combinedString);
  };

  const labelStyle: React.CSSProperties = {
    fontSize: "13px",
    fontWeight: 600,
    marginBottom: "6px",
  };

  const baseInput =
    "form-control bg-white border rounded-3 px-3 py-2 shadow-none";

  return (
    <div>
      <label style={labelStyle}>
        {label}
        {required && <span className="text-danger ms-1">*</span>}
      </label>
      <div className="d-flex gap-2">
        <select
          className="form-control bg-white border rounded-3 px-2 py-2"
          style={{ width: "110px", fontSize: "13px" }}
          value={countryCode}
          disabled={disabled}
          onChange={(e) => handlePhoneChange(e.target.value, phoneNumber)}
        >
          {countryCodes.map((code) => (
            <option key={code.value} value={code.value}>
              {code.label}
            </option>
          ))}
        </select>
        <input
          className={baseInput}
          style={{ flex: 1 }}
          type="tel"
          placeholder="Phone number"
          value={phoneNumber}
          disabled={disabled}
          onChange={(e) => handlePhoneChange(countryCode, e.target.value)}
        />
      </div>
      {error && (
        <div className="text-danger small mt-1" style={{ fontSize: "12px" }}>
          {error}
        </div>
      )}
    </div>
  );
};

// import { useEffect, useRef, useState } from "react";

// interface PhoneFieldProps {
//   label: string;
//   value: string;
//   onChange: (value: string) => void;
//   required?: boolean;
//   disabled?: boolean;
//   error?: string;
// }

// export const PhoneField: React.FC<PhoneFieldProps> = ({
//   label,
//   value,
//   onChange,
//   required = false,
//   disabled = false,
//   error,
// }) => {
//   const [countryCode, setCountryCode] = useState("+1");
//   const [phoneNumber, setPhoneNumber] = useState("");
//   const isInternalChange = useRef(false);

//   const countryCodes = [
//     { label: "+1 (US/CA)", value: "+1" },
//     { label: "+44 (UK)", value: "+44" },
//     { label: "+61 (AU)", value: "+61" },
//     { label: "+86 (CN)", value: "+86" },
//     { label: "+91 (IN)", value: "+91" },
//     { label: "+81 (JP)", value: "+81" },
//     { label: "+49 (DE)", value: "+49" },
//     { label: "+33 (FR)", value: "+33" },
//     { label: "+39 (IT)", value: "+39" },
//     { label: "+34 (ES)", value: "+34" },
//     { label: "+55 (BR)", value: "+55" },
//     { label: "+52 (MX)", value: "+52" },
//     { label: "+7 (RU)", value: "+7" },
//     { label: "+82 (KR)", value: "+82" },
//     { label: "+65 (SG)", value: "+65" },
//     { label: "+64 (NZ)", value: "+64" },
//     { label: "+63 (PH)", value: "+63" },
//     { label: "+353 (IE)", value: "+353" },
//     { label: "+31 (NL)", value: "+31" },
//     { label: "+46 (SE)", value: "+46" },
//     { label: "+47 (NO)", value: "+47" },
//   ];

//   useEffect(() => {
//     if (!isInternalChange.current && value) {
//       const match = value.match(/^(\+\d+)(.*)$/);
//       if (match) {
//         setCountryCode(match[1]);
//         setPhoneNumber(match[2]);
//       } else {
//         setPhoneNumber(value);
//       }
//     }
//     isInternalChange.current = false;
//   }, [value]);

//   const handlePhoneChange = (newCountryCode: string, newPhoneNumber: string) => {
//     isInternalChange.current = true;
//     setCountryCode(newCountryCode);
//     setPhoneNumber(newPhoneNumber);
//     onChange(`${newCountryCode}${newPhoneNumber}`);
//   };

//   const labelStyle: React.CSSProperties = {
//     fontSize: "13px",
//     fontWeight: 600,
//     marginBottom: "6px",
//   };

//   const baseInput =
//     "form-control bg-white border rounded-3 px-3 py-2 shadow-none";

//   return (
//     <div>
//       <label style={labelStyle}>
//         {label}
//         {required && <span className="text-danger ms-1">*</span>}
//       </label>
//       <div className="d-flex gap-2">
//         <select
//           className="form-control bg-white border rounded-3 px-2 py-2"
//           style={{ width: "110px", fontSize: "13px" }}
//           value={countryCode}
//           disabled={disabled}
//           onChange={(e) => handlePhoneChange(e.target.value, phoneNumber)}
//         >
//           {countryCodes.map((code) => (
//             <option key={code.value} value={code.value}>
//               {code.label}
//             </option>
//           ))}
//         </select>
//         <input
//           className={baseInput}
//           style={{ flex: 1 }}
//           type="tel"
//           placeholder="Phone number"
//           value={phoneNumber}
//           disabled={disabled}
//           onChange={(e) => handlePhoneChange(countryCode, e.target.value)}
//         />
//       </div>
//       {error && (
//         <div className="text-danger small mt-1" style={{ fontSize: "12px" }}>
//           {error}
//         </div>
//       )}
//     </div>
//   );
// };
