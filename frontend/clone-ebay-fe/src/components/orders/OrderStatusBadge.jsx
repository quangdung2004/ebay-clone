import { STATUS_LABELS, getStatusColorClass } from '../../utils/orderStatus';

const OrderStatusBadge = ({ status }) => {
  if (!status) return null;
  return (
    <span className={`order-status-badge ${getStatusColorClass(status)}`}>
      {STATUS_LABELS[status] || status}
    </span>
  );
};

export default OrderStatusBadge;
