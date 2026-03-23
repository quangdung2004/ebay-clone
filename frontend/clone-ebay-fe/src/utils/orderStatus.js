// Order Statuses
export const STATUS_LABELS = {
  PENDING_PAYMENT: 'Awaiting Payment',
  PAID: 'Paid',
  PROCESSING: 'Processing',
  SHIPPED: 'Shipped',
  DELIVERED: 'Delivered',
  COMPLETED: 'Completed',
  CANCELLED: 'Cancelled',
};

// Payment Method
export const PAYMENT_METHOD_LABELS = {
  
  PAYPAL: 'PayPal',
};

// Payment Statuses
export const PAYMENT_STATUS_LABELS = {
  PENDING: 'Pending',
  CAPTURED: 'Captured',
  CANCELLED: 'Cancelled',
};

// Settlement Statuses
export const SETTLEMENT_STATUS_LABELS = {
  PENDING: 'Pending',
  ON_HOLD: 'On Hold',
  AVAILABLE: 'Available',
  PAID_OUT: 'Paid Out',
  REVERSED: 'Reversed',
};

// Shipping Statuses
export const SHIPMENT_STATUS_LABELS = {
  PENDING: 'Pending',
  LABEL_CREATED: 'Label Created',
  IN_TRANSIT: 'In Transit',
  OUT_FOR_DELIVERY: 'Out for Delivery',
  DELIVERED: 'Delivered',
  EXCEPTION: 'Exception',
};

export function getStatusColorClass(status) {
  switch (status) {
    case 'PAID':
    case 'CAPTURED':
    case 'AVAILABLE':
    case 'PAID_OUT':
    case 'COMPLETED':
      return 'status-paid';
    case 'PENDING_PAYMENT':
    case 'PENDING':
    case 'LABEL_CREATED':
      return 'status-pending';
    case 'PROCESSING':
    case 'SHIPPED':
    case 'IN_TRANSIT':
    case 'OUT_FOR_DELIVERY':
      return 'status-processing';
    case 'ON_HOLD':
      return 'status-on-hold';
    case 'DELIVERED':
      return 'status-delivered';
    case 'CANCELLED':
    case 'REVERSED':
    case 'EXCEPTION':
      return 'status-cancelled';
    default:
      return 'status-default';
  }
}
