const ShippingInfoCard = ({ shippings }) => {
  if (!shippings || shippings.length === 0) {
    return (
      <div className="order-card-panel">
        <h3 className="order-section-title">Shipping Status</h3>
        <p className="order-text-muted">Shipping info is not available yet.</p>
      </div>
    );
  }

  return (
    <div className="order-card-panel">
      <h3 className="order-section-title">Shipping Status</h3>
      <div className="shipping-info-list">
        {shippings.map((s) => (
          <div className="shipping-info-item" key={s.id}>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Carrier:</span>
              <strong>{s.carrier || '--'}</strong>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Tracking:</span>
              <strong>{s.trackingNumber || '--'}</strong>
            </p>
            <p className="shipping-info-row">
              <span className="shipping-info-label">Status:</span>
              <strong>{s.status || 'Pending update'}</strong>
            </p>
            {s.estimatedArrival && (
              <p className="shipping-info-row">
                <span className="shipping-info-label">Est. Arrival:</span>
                <span>{new Date(s.estimatedArrival).toLocaleDateString()}</span>
              </p>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

export default ShippingInfoCard;
