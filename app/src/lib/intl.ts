export const intlCompactNumber = Intl.NumberFormat('en-US', {
  notation: 'compact',
  maximumFractionDigits: 2
});
export const intlFractionNumber = Intl.NumberFormat('en-US', {
  notation: 'standard',
  maximumFractionDigits: 2,
  minimumFractionDigits: 0,
  useGrouping: false
});
export const intlFixed4Number = Intl.NumberFormat('en-US', {
  notation: 'standard',
  maximumFractionDigits: 4,
  minimumFractionDigits: 4,
  useGrouping: false
});
export const intlWholeNumber = Intl.NumberFormat('en-US', {
  notation: 'compact',
  maximumFractionDigits: 0,
  useGrouping: false
})