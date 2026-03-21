import { formatCurrency } from '../../utils/productUtils';

const formatDate = (value) => {
  if (!value) return '--';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '--' : date.toLocaleDateString();
};

const ShippingInfoCard = ({ shipments }) => {
  if (!shipments || shipments.length === 0) {
    return (
      <div className="order-card-panel">
        <h3 className="order-section-title">Shipments</h3>
        <p className="order-text-muted">Shipment information is not available yet.</p>
      </div>
    );
  }

  return (
    <div className="order-card-panel">
      <h3 className="order-section-title">Shipments</h3>
      <div className="shipping-info-list">
        {shipments.map((shipment) => (
          <div className="shipping-info-item" key={shipment.id}>
            <div className="shipping-info-item-header">
              <strong>Shipment #{shipment.id}</strong>
              <span className="shipping-info-status">{shipment.status || 'Pending'}</span>
            </div>

            <p className="shipping-info-row">
              <span className="shipping-info-label">Seller:</span>
              <strong>#{shipment.sellerId}</strong>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Method:</span>
              <strong>{shipment.shippingMethod || '--'}</strong>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Carrier:</span>
              <strong>{shipment.carrier || '--'}</strong>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Tracking:</span>
              <strong>{shipment.trackingNumber || '--'}</strong>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Shipping Cost:</span>
              <strong>{formatCurrency(shipment.shippingCost || 0)}</strong>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Estimated Ship:</span>
              <span>{formatDate(shipment.estimatedShipDate)}</span>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Estimated Delivery:</span>
              <span>{formatDate(shipment.estimatedDeliveryDate)}</span>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Delivered At:</span>
              <span>{formatDate(shipment.deliveredAt)}</span>
            </p>
          </div>
        ))}
      </div>
    </div>
  );
};

export default ShippingInfoCard;