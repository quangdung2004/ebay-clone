import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useMyOrders } from '../hooks/useMyOrders';
import OrderStatusBadge from '../components/orders/OrderStatusBadge';
import { cancelOrder } from '../api/orderApi';
import { useToast } from '../components/Toast';
import { formatCurrency, getPlaceholderImage, normalizeProductImageUrl } from '../utils/productUtils';
import './MyOrdersPage.css';

const TABS = [
  { key: 'ALL', label: 'All Orders' },
  { key: 'PENDING_PAYMENT', label: 'Pending Payment' },
  { key: 'CONFIRMED', label: 'Confirmed' },
  { key: 'PAID', label: 'Paid' },
  { key: 'CANCELLED', label: 'Cancelled' },
];

const getDisplayTotal = (order) => Number(order?.grandTotal || order?.totalPrice || 0);

const MyOrdersPage = () => {
  const [activeTab, setActiveTab] = useState('ALL');
  const [currentPage, setCurrentPage] = useState(1);
  const { orders, loading, error, refreshOrders } = useMyOrders(currentPage, 50);
  const [actionLoadingId, setActionLoadingId] = useState(false);
  const { showSuccess, showError } = useToast();

  const filteredOrders = useMemo(() => {
    if (activeTab === 'ALL') return orders;
    return orders.filter((o) => o.status === activeTab);
  }, [orders, activeTab]);

  const summary = useMemo(() => {
    const s = { TOTAL: orders.length, PENDING_PAYMENT: 0, PAID: 0, CANCELLED: 0 };
    orders.forEach((o) => {
      if (o.status === 'PENDING_PAYMENT') s.PENDING_PAYMENT++;
      else if (o.status === 'PAID') s.PAID++;
      else if (o.status === 'CANCELLED') s.CANCELLED++;
    });
    return s;
  }, [orders]);

  const handleCancel = async (orderId) => {
    const ok = window.confirm('Are you sure you want to cancel this order?');
    if (!ok) return;

    try {
      setActionLoadingId(orderId);
      await cancelOrder(orderId);
      showSuccess(`Order #${orderId} cancelled successfully`);
      refreshOrders();
    } catch (err) {
      showError(err.message || 'Failed to cancel the order');
    } finally {
      setActionLoadingId(null);
    }
  };

  const getLatestPayment = (order) => {
    if (!order.payments || !order.payments.length) return null;
    return order.payments[order.payments.length - 1];
  };

  return (
    <div className="my-orders-page">
      <div className="my-orders-header">
        <div>
          <span className="my-orders-eyebrow">Purchase History</span>
          <h1>My Orders</h1>
        </div>
        <div className="my-orders-summary-cards">
          <div className="orders-summary-card">
            <span>All Orders</span><strong>{summary.TOTAL}</strong>
          </div>
          <div className="orders-summary-card">
            <span>Awaiting Payment</span><strong>{summary.PENDING_PAYMENT}</strong>
          </div>
          <div className="orders-summary-card">
            <span>Paid</span><strong>{summary.PAID}</strong>
          </div>
        </div>
      </div>

      <div className="my-orders-tabs">
        {TABS.map((tab) => (
          <button
            key={tab.key}
            className={`my-orders-tab ${activeTab === tab.key ? 'active' : ''}`}
            onClick={() => setActiveTab(tab.key)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="my-orders-loading">Loading your orders...</div>
      ) : error ? (
        <div className="my-orders-error">Failed to load orders. Please try again.</div>
      ) : filteredOrders.length === 0 ? (
        <div className="my-orders-empty">
          <h3>No orders found</h3>
          <p>You have no orders matching this filter.</p>
          <Link to="/" className="btn-shop-now">Shop Now</Link>
        </div>
      ) : (
        <div className="my-orders-list">
          {filteredOrders.map((order) => {
            const latestPayment = getLatestPayment(order);
            const canCancel = order.status === 'PENDING_PAYMENT' || order.status === 'CONFIRMED';
            const isPayPalPending = order.status === 'PENDING_PAYMENT' && latestPayment?.method === 'PAYPAL';

            return (
              <div className="my-order-card" key={order.id}>
                <div className="my-order-card-header">
                  <div className="my-order-meta">
                    <div className="my-order-meta-info">
                      <span className="my-order-label">Order placed on</span>
                      <span className="my-order-value">{new Date(order.orderDate).toLocaleDateString()}</span>
                    </div>
                    <div className="my-order-meta-info">
                      <span className="my-order-label">Total Amount</span>
                      <span className="my-order-value">{formatCurrency(getDisplayTotal(order))}</span>
                    </div>
                  </div>
                  <div className="my-order-status-block">
                    <span className="my-order-number">Order #{order.id}</span>
                    <OrderStatusBadge status={order.status} />
                  </div>
                </div>

                <div className="my-order-card-body">
                  <div className="my-order-items">
                    {order.items?.map((item) => (
                      <div className="my-order-item" key={item.id}>
                        <img src={normalizeProductImageUrl(item.thumbnailUrl) || getPlaceholderImage()} alt={item.productTitle} />
                        <div className="my-order-item-details">
                          <Link to={`/products/${item.productId}`} className="my-order-item-title">{item.productTitle}</Link>
                          <span className="my-order-item-qty">Qty: {item.quantity}</span>
                        </div>
                      </div>
                    ))}
                  </div>

                  <div className="my-order-actions">
                    <div className="my-order-total-breakdown">
                      <span>Items: {formatCurrency(order.itemSubtotal || 0)}</span>
                      <span>Shipping: {formatCurrency(order.shippingTotal || 0)}</span>
                    </div>

                    <Link to={`/orders/${order.id}`} className="btn-view-detail">View Order Details</Link>

                    {isPayPalPending && (
                      <Link to={`/orders/${order.id}`} className="btn-pay-now-action">Pay Now</Link>
                    )}

                    {canCancel && (
                      <button
                        className="btn-cancel-action"
                        onClick={() => handleCancel(order.id)}
                        disabled={actionLoadingId === order.id}
                      >
                        {actionLoadingId === order.id ? 'Cancelling...' : 'Cancel Order'}
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default MyOrdersPage;