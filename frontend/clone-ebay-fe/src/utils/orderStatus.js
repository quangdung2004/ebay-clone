export const STATUS_LABELS = {
  PENDING_PAYMENT: 'Awaiting Payment',
  CONFIRMED: 'Confirmed',
  PAID: 'Paid',
  PROCESSING: 'Seller Preparing Package',
  SHIPPED: 'Shipped',
  DELIVERED: 'Delivered',
  CANCELLED: 'Cancelled',
};

export const PAYMENT_METHOD_LABELS = {
  COD: 'Cash on Delivery',
  PAYPAL: 'PayPal',
};

export const PAYMENT_STATUS_LABELS = {
  PENDING: 'Pending',
  CAPTURED: 'Captured',
  PAID: 'Paid',
  CANCELLED: 'Cancelled',
};

export const SETTLEMENT_STATUS_LABELS = {
  ON_HOLD: 'On Hold',
  AVAILABLE: 'Available',
  PAID_OUT: 'Paid Out',
  REFUNDED: 'Refunded',
};

export function getStatusColorClass(status) {
  switch (status) {
    case 'PAID':
    case 'CAPTURED':
    case 'AVAILABLE':
    case 'PAID_OUT':
      return 'status-paid';

    case 'PROCESSING':
    case 'CONFIRMED':
      return 'status-confirmed';

    case 'SHIPPED':
      return 'status-on-hold';

    case 'DELIVERED':
      return 'status-paid';

    case 'PENDING_PAYMENT':
    case 'PENDING':
      return 'status-pending';

    case 'ON_HOLD':
      return 'status-on-hold';

    case 'CANCELLED':
    case 'REFUNDED':
      return 'status-cancelled';

    default:
      return 'status-default';
  }
}