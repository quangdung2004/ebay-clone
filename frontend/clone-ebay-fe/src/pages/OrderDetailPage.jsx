import { useState } from 'react';
import { useParams, useLocation, Link } from 'react-router-dom';
import { useOrderDetail } from '../hooks/useOrderDetail';
import { useAuth } from '../context/AuthContext';
import OrderStatusBadge from '../components/orders/OrderStatusBadge';
import OrderItemList from '../components/orders/OrderItemList';
import OrderAddressCard from '../components/orders/OrderAddressCard';
import PaymentHistoryCard from '../components/orders/PaymentHistoryCard';
import ShippingInfoCard from '../components/orders/ShippingInfoCard';
import PayPalButtonsSection from '../components/payments/PayPalButtonsSection';
import OrderActionBar from '../components/orders/OrderActionBar';
import ChangeAddressModal from '../components/orders/ChangeAddressModal';
import { formatCurrency, formatDateTime } from '../utils/productUtils';
import './OrderDetailPage.css';

const OrderDetailPage = () => {
  const { id } = useParams();
  const location = useLocation();
  const { user } = useAuth();
  const [isChangeAddressOpen, setIsChangeAddressOpen] = useState(false);
  
  const trySellerFirst = location.state?.trySellerFirst || false;
  const { order, loading, error, refreshOrder } = useOrderDetail(id, trySellerFirst);

  // Handle potential JSON casing differences
  const currentUserId = user?.id || user?.Id;

  const isSeller = order?.items?.some(item => String(item.sellerId) === String(currentUserId)) || false;
  const isBuyer = String(order?.buyerId) === String(currentUserId);

  // Multi-seller logic: if user is only a seller in this order, filter items to show only theirs
  const viewAsSeller = isSeller && !isBuyer;
  const visibleItems = viewAsSeller 
    ? order?.items?.filter(item => String(item.sellerId) === String(currentUserId)) || [] 
    : order?.items || [];
    
  // Status check for address change (sync with backend)
  const forbiddenChangeStatuses = ['SHIPPED', 'DELIVERED', 'COMPLETED', 'CANCELLED'];
  const isStatusEligibleForChange = !forbiddenChangeStatuses.includes(String(order?.status).toUpperCase());
  
  const canChangeAddress = isBuyer && 
                           (order?.addressChangeCount || 0) < 1 && 
                           isStatusEligibleForChange;

  const displayTotal = viewAsSeller 
    ? visibleItems.reduce((sum, item) => sum + item.lineTotal, 0) + (order.shippingFee || 0)
    : order?.totalPrice;

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
        <Link to={viewAsSeller ? "/seller/orders" : "/orders"} className="back-link">
          ← Back to {viewAsSeller ? "my sales" : "orders"}
        </Link>
        <div className="order-detail-header">
           <div className="order-title">
             <h1>Order #{order.id}</h1>
             <OrderStatusBadge status={order.status} />
           </div>
           <p className="order-date">Placed on {new Date(order.orderDate).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric'})}</p>
           {viewAsSeller && (
             <div className="order-seller-revenue">
                <span style={{ fontSize: '1.1rem', color: '#1f2937' }}>
                   My Portion Revenue: <strong style={{color: 'var(--primary-color)'}}>{formatCurrency(displayTotal)}</strong>
                </span>
             </div>
           )}
        </div>
      </div>

      <div className="order-detail-grid">
         <div className="order-detail-main">
            {isBuyer && <PayPalButtonsSection order={order} onPaymentSuccess={refreshOrder} />}
            <OrderItemList 
               items={visibleItems} 
               subtotalAmount={viewAsSeller ? visibleItems.reduce((s, i) => s + i.lineTotal, 0) : order.subtotalAmount}
               shippingFee={order.shippingFee}
               totalPrice={displayTotal}
            />
            <OrderActionBar 
               order={order} 
               isSeller={isSeller}
               isBuyer={isBuyer}
               onOrderUpdated={refreshOrder} 
            />
         </div>
         
         <div className="order-detail-sidebar">
            <OrderAddressCard 
               address={order.address} 
               canChange={canChangeAddress}
               onChangeClick={() => setIsChangeAddressOpen(true)}
               changeCount={order.addressChangeCount}
               lastChangedAt={order.lastAddressChangedAt}
            />
            <PaymentHistoryCard payments={order.payments || []} />
            <ShippingInfoCard shippings={order.shippings} orderId={order.id} orderStatus={order.status} />
         </div>
      </div>

      {/* Modals */}
      <ChangeAddressModal 
         isOpen={isChangeAddressOpen}
         onClose={() => setIsChangeAddressOpen(false)}
         orderId={order.id}
         currentAddressId={order.addressId}
         onUpdated={refreshOrder}
      />
    </div>
  );
};

export default OrderDetailPage;
