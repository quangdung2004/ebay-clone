import { useState } from 'react';
import { cancelOrder } from '../../api/orderApi';
import { useToast } from '../Toast';
import { PAYMENT_METHOD_LABELS } from '../../utils/orderStatus';

const OrderActionBar = ({ order, onOrderCancelled }) => {
  const [isCancelling, setIsCancelling] = useState(false);
  const { showSuccess, showError } = useToast();

  if (!order) return null;

  const validToCancel = order.status === 'PENDING_PAYMENT' || order.status === 'CONFIRMED';
  
  const latestPayment = order.payments && order.payments.length > 0
    ? order.payments[order.payments.length - 1]
    : {};

  const handleCancelClick = async () => {
    const isConfirmed = window.confirm('Are you sure you want to cancel this order?');
    if (!isConfirmed) return;

    try {
      setIsCancelling(true);
      await cancelOrder(order.id);
      showSuccess('Order has been cancelled successfully');
      if (onOrderCancelled) onOrderCancelled();
    } catch (err) {
      showError(err.message || 'Failed to cancel the order');
    } finally {
      setIsCancelling(false);
    }
  };

  return (
    <div className="order-action-bar">
       {(order.status === 'PAID' || latestPayment?.status === 'CAPTURED') && (
         <div className="order-message-success">
           This order has been successfully paid.
         </div>
       )}
       {order.status === 'CONFIRMED' && latestPayment?.method === 'COD' && (
         <div className="order-message-info">
           Method: {PAYMENT_METHOD_LABELS['COD']} - Please prepare cash for delivery.
         </div>
       )}
       {order.status === 'CANCELLED' && (
         <div className="order-message-error">
           This order is cancelled.
         </div>
       )}

       <div className="order-action-buttons">
         {validToCancel && (
           <button
             type="button"
             className="btn-order-cancel"
             onClick={handleCancelClick}
             disabled={isCancelling}
           >
             {isCancelling ? 'Cancelling...' : 'Cancel Order'}
           </button>
         )}
       </div>
    </div>
  );
};

export default OrderActionBar;
