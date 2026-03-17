import { formatCurrency, getPlaceholderImage, normalizeProductImageUrl } from '../../utils/productUtils';
import { Link } from 'react-router-dom';

const OrderItemList = ({ items }) => {
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
    </div>
  );
};

export default OrderItemList;
