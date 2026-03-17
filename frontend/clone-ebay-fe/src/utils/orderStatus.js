export const STATUS_LABELS = {
  PENDING_PAYMENT: 'Awaiting Payment',
  CONFIRMED: 'Confirmed',
  PAID: 'Paid',
  CANCELLED: 'Cancelled',
};

export const PAYMENT_METHOD_LABELS = {
  COD: 'Cash on Delivery',
  PAYPAL: 'PayPal',
};

export const PAYMENT_STATUS_LABELS = {
  PENDING: 'Pending',
  PAID: 'Paid',
  CANCELLED: 'Cancelled',
};

export function getStatusColorClass(status) {
  switch (status) {
    case 'PAID':
      return 'status-paid';
    case 'PENDING_PAYMENT':
      return 'status-pending';
    case 'CONFIRMED':
      return 'status-confirmed';
    case 'CANCELLED':
      return 'status-cancelled';
    default:
      return 'status-default';
  }
}
