import { formatCurrency, getPlaceholderImage, normalizeProductImageUrl } from '../../utils/productUtils';
import { Link } from 'react-router-dom';

const OrderItemList = ({ items, subtotalAmount = 0, shippingFee = 0, totalPrice = 0 }) => {
  if (!items || items.length === 0) {
    return <div className="order-items-empty">No items found in this order.</div>;
  }

  return (
    <div className="order-items-list">
      <h3 className="order-section-title">Purchased Items</h3>
      <div className="order-items-container">
        {items.map((item) => (
          <div className="order-detail-item" key={item.id}>
            <div className="order-detail-image-wrap">
              <Link to={`/products/${item.productId}`}>
                <img
                  src={normalizeProductImageUrl(item.thumbnailUrl) || getPlaceholderImage()}
                  alt={item.productTitle}
                  className="order-detail-image"
                />
              </Link>
            </div>
            <div className="order-detail-content">
              <Link to={`/products/${item.productId}`} className="order-detail-title-link">
                <h4 className="order-detail-title">{item.productTitle}</h4>
              </Link>
              <div className="order-detail-meta">
                <span>Qty: {item.quantity}</span>
                <span>Unit Price: {formatCurrency(item.unitPrice)}</span>
                <span>Seller: {item.sellerName || 'Unknown seller'}</span>
              </div>
              <div className="order-detail-total">
                Subtotal: <strong>{formatCurrency(item.lineTotal)}</strong>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Price Summary Breakdown */}
      <div className="order-price-summary" style={{ marginTop: '2rem', paddingTop: '1.5rem', borderTop: '2px solid #f3f4f6' }}>
        <div style={{ maxWidth: '300px', marginLeft: 'auto' }}>
           <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem', color: '#4b5563' }}>
              <span>Subtotal:</span>
              <span>{formatCurrency(subtotalAmount)}</span>
           </div>
           <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem', color: '#4b5563' }}>
              <span>Shipping Fee:</span>
              <span>{formatCurrency(shippingFee)}</span>
           </div>
           <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '1rem', paddingTop: '1rem', borderTop: '1px solid #e5e7eb', fontSize: '1.2rem', fontWeight: 'bold', color: '#111827' }}>
              <span>Total:</span>
              <span style={{ color: 'var(--primary-color)' }}>{formatCurrency(totalPrice)}</span>
           </div>
        </div>
      </div>
    </div>
  );
};

export default OrderItemList;
