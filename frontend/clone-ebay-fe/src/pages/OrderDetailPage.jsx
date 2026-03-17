import { useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useOrderDetail } from '../hooks/useOrderDetail';
import OrderStatusBadge from '../components/orders/OrderStatusBadge';
import OrderItemList from '../components/orders/OrderItemList';
import OrderAddressCard from '../components/orders/OrderAddressCard';
import PaymentHistoryCard from '../components/orders/PaymentHistoryCard';
import ShippingInfoCard from '../components/orders/ShippingInfoCard';
import PayPalButtonsSection from '../components/payments/PayPalButtonsSection';
import OrderActionBar from '../components/orders/OrderActionBar';
import './OrderDetailPage.css';

const OrderDetailPage = () => {
  const { id } = useParams();
  const { order, loading, error, refreshOrder } = useOrderDetail(id);

  if (loading) {
    return (
      <div className="order-detail-page">
        <div className="order-detail-loading">Loading order details...</div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="order-detail-page">
        <div className="order-detail-error">
          <h3>Oops! Order not found.</h3>
          <p>{error?.message || 'We could not find the order details.'}</p>
          <Link to="/orders" className="btn-back">Back to Orders</Link>
        </div>
      </div>
    );
  }

  return (
    <div className="order-detail-page">
      <div className="order-detail-header-wrap">
        <Link to="/orders" className="back-link">← Back to orders</Link>
        <div className="order-detail-header">
           <div className="order-title">
             <h1>Order #{order.id}</h1>
             <OrderStatusBadge status={order.status} />
           </div>
           <p className="order-date">Placed on {new Date(order.orderDate).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric'})}</p>
        </div>
      </div>

      <div className="order-detail-grid">
         <div className="order-detail-main">
            <PayPalButtonsSection order={order} onPaymentSuccess={refreshOrder} />
            <OrderItemList items={order.items || []} />
            <OrderActionBar order={order} onOrderCancelled={refreshOrder} />
         </div>
         
         <div className="order-detail-sidebar">
            <OrderAddressCard address={order.address} />
            <PaymentHistoryCard payments={order.payments || []} />
            <ShippingInfoCard shippings={order.shippings || []} />
         </div>
      </div>
    </div>
  );
};

export default OrderDetailPage;
