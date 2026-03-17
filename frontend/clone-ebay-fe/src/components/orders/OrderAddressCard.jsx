const OrderAddressCard = ({ address }) => {
  if (!address) {
    return (
      <div className="order-card-panel">
        <h3 className="order-section-title">Shipping Address</h3>
        <p className="order-text-muted">No shipping address provided.</p>
      </div>
    );
  }

  return (
    <div className="order-card-panel">
      <h3 className="order-section-title">Shipping Address</h3>
      <div className="order-address-box">
        <p className="order-address-name">
          <strong>{address.fullName}</strong>
          {address.phone && <span className="order-address-phone">{address.phone}</span>}
        </p>
        <p className="order-address-line">{address.street}</p>
        <p className="order-address-line">
          {address.city}, {address.state}
        </p>
        <p className="order-address-line">{address.country}</p>
      </div>
    </div>
  );
};

export default OrderAddressCard;
