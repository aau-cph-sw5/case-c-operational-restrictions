// Date helpers for the restriction views.
// The backend serialises DateTime without a "Z" when Kind is Unspecified, so
// values without a timezone designator are treated as UTC, not local time.
const HAS_TIMEZONE = /(Z|[+-]\d{2}:?\d{2})$/i;

export function parseApiDate(value: string): Date {
  return new Date(HAS_TIMEZONE.test(value) ? value : `${value}Z`);
}

const pad = (n: number) => String(n).padStart(2, "0");

// YYYY-MM-DD HH:mm:ss UTC
export function formatUtc(value: string): string {
  const date = parseApiDate(value);
  if (Number.isNaN(date.getTime())) return "Invalid date";

  return (
    `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())} ` +
    `${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())}:${pad(date.getUTCSeconds())} UTC`
  );
}

// Browser-local time"
export function formatLocal(value: string): string {
  const date = parseApiDate(value);
  if (Number.isNaN(date.getTime())) return "Invalid date";

  const time = new Intl.DateTimeFormat(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
    timeZoneName: "short",
  }).format(date);

  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ` +
    time
  );
}
