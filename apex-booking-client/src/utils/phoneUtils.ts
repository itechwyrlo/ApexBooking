export const COUNTRY_CODES = [
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

export function parsePhoneValue(value: string): { countryCode: string; phoneNumber: string } {
  const cleanValue = value.startsWith("+") ? value : `+${value}`;
  const sortedCodes = [...COUNTRY_CODES].sort((a, b) => b.value.length - a.value.length);
  const matchedCode = sortedCodes.find((c) => cleanValue.startsWith(c.value));

  if (matchedCode) {
    return { countryCode: matchedCode.value, phoneNumber: cleanValue.slice(matchedCode.value.length) };
  }
  return { countryCode: "+1", phoneNumber: value };
}
