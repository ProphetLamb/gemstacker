export const intlCompactNumber = Intl.NumberFormat('en-US', {
  notation: 'compact',
  maximumFractionDigits: 2
});
export const intlFractionNumber = Intl.NumberFormat('en-US', {
  notation: 'standard',
  maximumFractionDigits: 2,
  minimumFractionDigits: 0
});