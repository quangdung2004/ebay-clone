import { useEffect, useMemo, useState } from 'react';
import { cancelOrder, getMyOrders, payOrder } from '../api/orderApi';
import { useToast } from '../components/Toast';
import './OrdersPage.css';

const STATUS_LABELS = {
  PENDING_PAYMENT: 'Awaiting payment',
  CONFIRMED: 'Confirmed',
  PAID: 'Paid',
  CANCELLED: 'Cancelled',
};

const STATUS_TABS = [
  { key: 'ALL', label: 'All orders' },
  { key: 'PENDING_PAYMENT', label: 'Awaiting payment' },
  { key: 'CONFIRMED', label: 'Confirmed' },
  { key: 'PAID', label: 'Paid' },
  { key: 'CANCELLED', label: 'Cancelled' },
];

function formatPrice(value) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(Number(value || 0));
}

function formatDate(dateString) {
  if (!dateString) return '--';
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

function getStatusClass(status) {
  switch (status) {
    case 'PAID':
      return 'paid';
    case 'PENDING_PAYMENT':
      return 'pending';
    case 'CONFIRMED':
      return 'confirmed';
    case 'CANCELLED':
      return 'cancelled';
    default:
      return 'default';
  }
}

function getLatestPayment(payments = []) {
  if (!payments.length) return null;
  return payments[payments.length - 1];
}

const OrdersPage = () => {
  const [orders, setOrders] = useState([]);
  const [activeTab, setActiveTab] = useState('ALL');
  const [loading, setLoading] = useState(true);
  const [actionLoadingId, setActionLoadingId] = useState(null);
  const { showSuccess, showError } = useToast();

  const fetchOrders = async () => {
    try {
      setLoading(true);
      const data = await getMyOrders(1, 50);
      setOrders(data?.items || []);
    } catch (error) {
      showError(error.message || 'Failed to load orders');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchOrders();
  }, []);

  const filteredOrders = useMemo(() => {
    if (activeTab === 'ALL') return orders;
    return orders.filter((order) => order.status === activeTab);
  }, [orders, activeTab]);

  const summary = useMemo(() => {
    return {
      total: orders.length,
      awaitingPayment: orders.filter((o) => o.status === 'PENDING_PAYMENT').length,
      paid: orders.filter((o) => o.status === 'PAID').length,
      cancelled: orders.filter((o) => o.status === 'CANCELLED').length,
    };
  }, [orders]);

  const handlePay = async (orderId) => {
    try {
      setActionLoadingId(orderId);
      await payOrder(orderId);
      showSuccess('Order paid successfully');
      await fetchOrders();
    } catch (error) {
      showError(error.message || 'Failed to pay order');
    } finally {
      setActionLoadingId(null);
    }
  };

  const handleCancel = async (orderId) => {
    const confirmed = window.confirm('Are you sure you want to cancel this order?');
    if (!confirmed) return;

    try {
      setActionLoadingId(orderId);
      await cancelOrder(orderId);
      showSuccess('Order cancelled successfully');
      await fetchOrders();
    } catch (error) {
      showError(error.message || 'Failed to cancel order');
    } finally {
      setActionLoadingId(null);
    }
  };

  return (
    <div className="orders-page">
      <div className="orders-shell">
        <div className="orders-hero">
          <div>
            <p className="orders-eyebrow">Purchase history</p>
            <h1 className="orders-title">Your orders</h1>
            <p className="orders-subtitle">
              Track purchases, review payment status, and manage recent orders.
            </p>
          </div>

          <div className="orders-summary">
            <div className="orders-summary-card">
              <span className="orders-summary-label">All</span>
              <strong>{summary.total}</strong>
            </div>
            <div className="orders-summary-card">
              <span className="orders-summary-label">Awaiting payment</span>
              <strong>{summary.awaitingPayment}</strong>
            </div>
            <div className="orders-summary-card">
              <span className="orders-summary-label">Paid</span>
              <strong>{summary.paid}</strong>
            </div>
            <div className="orders-summary-card">
              <span className="orders-summary-label">Cancelled</span>
              <strong>{summary.cancelled}</strong>
            </div>
          </div>
        </div>

        <div className="orders-tabs">
          {STATUS_TABS.map((tab) => (
            <button
              key={tab.key}
              className={`orders-tab ${activeTab === tab.key ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.key)}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {loading ? (
          <div className="orders-empty">Loading orders...</div>
        ) : filteredOrders.length === 0 ? (
          <div className="orders-empty">
            <h3>No orders found</h3>
            <p>You do not have any orders in this category yet.</p>
          </div>
        ) : (
          <div className="orders-list">
            {filteredOrders.map((order) => {
              const latestPayment = getLatestPayment(order.payments);
              const canPay = order.status === 'PENDING_PAYMENT';
              const canCancel =
                order.status === 'PENDING_PAYMENT' || order.status === 'CONFIRMED';

              return (
                <article className="order-card" key={order.id}>
                  <div className="order-card-top">
                    <div className="order-meta-group">
                      <div className="order-meta-item">
                        <span className="order-meta-label">Order placed</span>
                        <strong>{formatDate(order.orderDate)}</strong>
                      </div>

                      <div className="order-meta-item">
                        <span className="order-meta-label">Total</span>
                        <strong>{formatPrice(order.totalPrice)}</strong>
                      </div>

                      <div className="order-meta-item">
                        <span className="order-meta-label">Items</span>
                        <strong>{order.totalItems}</strong>
                      </div>
                    </div>

                    <div className="order-meta-right">
                      <span className={`order-status-badge ${getStatusClass(order.status)}`}>
                        {STATUS_LABELS[order.status] || order.status}
                      </span>
                      <span className="order-number">Order #{order.id}</span>
                    </div>
                  </div>

                  <div className="order-card-body">
                    <div className="order-items">
                      {order.items?.map((item) => (
                        <div className="order-item" key={item.id}>
                          <div className="order-item-image-wrap">
                            <img
                              src={
                                item.thumbnailUrl ||
                                'https://placehold.co/96x96?text=No+Image'
                              }
                              alt={item.productTitle}
                              className="order-item-image"
                            />
                          </div>

                          <div className="order-item-content">
                            <h3 className="order-item-title">{item.productTitle}</h3>

                            <div className="order-item-meta">
                              <span>Qty: {item.quantity}</span>
                              <span>Unit price: {formatPrice(item.unitPrice)}</span>
                              <span>Seller: {item.sellerName || 'Unknown seller'}</span>
                            </div>

                            <div className="order-item-total">
                              Item total: {formatPrice(item.lineTotal)}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>

                    <aside className="order-side-panel">
                      <div className="order-side-box">
                        <h4>Payment</h4>
                        <p>
                          <span>Method:</span>{' '}
                          <strong>{latestPayment?.method || '--'}</strong>
                        </p>
                        <p>
                          <span>Status:</span>{' '}
                          <strong>{latestPayment?.status || '--'}</strong>
                        </p>
                        <p>
                          <span>Paid at:</span>{' '}
                          <strong>{formatDate(latestPayment?.paidAt)}</strong>
                        </p>
                      </div>

                      <div className="order-side-box">
                        <h4>Actions</h4>

                        {canPay && (
                          <button
                            className="order-action primary"
                            onClick={() => handlePay(order.id)}
                            disabled={actionLoadingId === order.id}
                          >
                            {actionLoadingId === order.id ? 'Processing...' : 'Pay now'}
                          </button>
                        )}

                        {canCancel && (
                          <button
                            className="order-action secondary"
                            onClick={() => handleCancel(order.id)}
                            disabled={actionLoadingId === order.id}
                          >
                            {actionLoadingId === order.id
                              ? 'Processing...'
                              : 'Cancel order'}
                          </button>
                        )}

                        {!canPay && !canCancel && (
                          <p className="order-action-note">
                            No more actions available for this order.
                          </p>
                        )}
                      </div>
                    </aside>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};

export default OrdersPage;