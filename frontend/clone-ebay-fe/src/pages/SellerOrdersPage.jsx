import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useSellerOrders } from '../hooks/useSellerOrders';
import { useAuth } from '../context/AuthContext';
import { formatCurrency, formatDateTime } from '../utils/productUtils';
import { getStatusColorClass, STATUS_LABELS } from '../utils/orderStatus';
import OrderStatusBadge from '../components/orders/OrderStatusBadge';
import './SellerOrdersPage.css';

const TABS = [
  { id: 'ALL', label: 'All Orders' },
  { id: 'PAID', label: 'To Process (Paid)' },
  { id: 'PROCESSING', label: 'To Ship' },
  { id: 'SHIPPED', label: 'In Transit' },
  { id: 'DELIVERED', label: 'Delivered' }
];

const SellerOrdersPage = () => {
  const { user } = useAuth();
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const { data, loading, error } = useSellerOrders(page, pageSize);
  const [activeTab, setActiveTab] = useState('ALL');

  const filteredOrders = useMemo(() => {
    if (!data || !data.items) return [];
    if (activeTab === 'ALL') return data.items;
    return data.items.filter((order) => order.status === activeTab);
  }, [data, activeTab]);

  const totalPages = Math.ceil((data?.total || 0) / pageSize);

  const handlePageChange = (newPage) => {
    if (newPage > 0 && newPage <= totalPages) {
      setPage(newPage);
    }
  };

  if (loading && page === 1) {
    return <div className="seller-orders-page"><div className="orders-loading">Loading your orders...</div></div>;
  }

  if (error) {
    return (
      <div className="seller-orders-page">
        <div className="orders-error">
          <h3>Failed to load orders</h3>
          <p>{error?.message || 'Please try again later.'}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="seller-orders-page">
      <div className="orders-header">
        <h1>My Sales (Seller Orders)</h1>
        <p>Manage fulfillments, shipments, and customer orders</p>
      </div>

      <div className="orders-tabs">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            className={`tab-btn ${activeTab === tab.id ? 'active' : ''}`}
            onClick={() => {
              setActiveTab(tab.id);
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className="orders-list">
        {filteredOrders.length === 0 ? (
          <div className="no-orders-empty">
            <p>No orders found for this status.</p>
          </div>
        ) : (
          filteredOrders.map((order) => {
            // Find items sold by the current seller in this order
            const currentUserId = user?.id || user?.Id;
            const myItems = order.items.filter(i => String(i.sellerId) === String(currentUserId));
            const myTotal = myItems.reduce((acc, i) => acc + i.lineTotal, 0) + (order.shippingFee || 0);

            return (
              <div key={order.id} className="order-card">
                <div className="order-card-header">
                  <div className="order-meta">
                    <strong>Order O-#{order.id}</strong>
                    <span className="order-date">Placed: {formatDateTime(order.orderDate)}</span>
                  </div>
                  <div className="order-status-wrapper">
                    <OrderStatusBadge status={order.status} />
                  </div>
                </div>

                <div className="order-card-body">
                  <div className="order-customer-info">
                    <span>Buyer: <strong>{order.buyerName || 'Guest'}</strong></span>
                  </div>
                  <div className="order-items-preview">
                    {myItems.map((item) => (
                      <div key={item.id} className="preview-item">
                        <span className="preview-item-qty">{item.quantity}x</span>
                        <span className="preview-item-title">{item.productTitle}</span>
                        <span className="preview-item-price">{formatCurrency(item.unitPrice)}</span>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="order-card-footer">
                  <div className="order-totals">
                    <span>My Revenue: <strong>{formatCurrency(myTotal)}</strong></span>
                  </div>
                  <div className="order-actions">
                    <Link to={`/orders/${order.id}`} state={{ trySellerFirst: true }} className="btn-view-details">
                      View Details / Ship →
                    </Link>
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>

      {totalPages > 1 && (
        <div className="pagination">
          <button 
            onClick={() => handlePageChange(page - 1)} 
            disabled={page === 1}
            className="btn-page"
          >
            Previous
          </button>
          <span className="page-info">
            Page {page} of {totalPages}
          </span>
          <button 
            onClick={() => handlePageChange(page + 1)} 
            disabled={page === totalPages}
            className="btn-page"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
};

export default SellerOrdersPage;
