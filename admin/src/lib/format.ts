const currencyFormatter = new Intl.NumberFormat("en-ZA", { style: "currency", currency: "ZAR" });
const dateTimeFormatter = new Intl.DateTimeFormat("en-ZA", { dateStyle: "medium", timeStyle: "short" });
const dateFormatter = new Intl.DateTimeFormat("en-ZA", { dateStyle: "medium" });
const percentFormatter = new Intl.NumberFormat("en-ZA", { style: "percent", minimumFractionDigits: 1, maximumFractionDigits: 1 });

export function formatCurrency(amount: number): string {
  return currencyFormatter.format(amount);
}

export function formatDateTime(isoDate: string): string {
  return dateTimeFormatter.format(new Date(isoDate));
}

export function formatDate(isoDate: string): string {
  return dateFormatter.format(new Date(isoDate));
}

export function formatPercent(fraction: number): string {
  return percentFormatter.format(fraction);
}
