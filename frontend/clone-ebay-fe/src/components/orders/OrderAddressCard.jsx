const OrderAddressCard = ({
  address,
  canUpdateAddress = false,
  remainingAddressChanges = 0,
  onEditAddress,
  isUpdatingAddress = false,
}) => {
  const helperText = canUpdateAddress
    ? 'You can update the shipping address before shipment starts.'
    : remainingAddressChanges === 0
      ? 'You have used the one allowed address change for this paid order.'
      : 'Shipping address can no longer be changed for this order.';

  return (
    <div className="order-card-panel">
      <div className="order-section-header-inline">
        <h3 className="order-section-title order-section-title-no-border">Shipping Address</h3>

        {canUpdateAddress && (
          <button
            type="button"
            className="btn-order-address-edit"
            onClick={onEditAddress}
            disabled={isUpdatingAddress}
          >
            {isUpdatingAddress ? 'Updating...' : 'Change address'}
          </button>
        )}
      </div>

      {address ? (
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
      ) : (
        <p className="order-text-muted">No shipping address provided.</p>
      )}

      <p className="order-address-note">{helperText}</p>

      {remainingAddressChanges > 0 && (
        <p className="order-address-meta">
          Remaining paid address changes: {remainingAddressChanges}
        </p>
      )}
    </div>
  );
};

export default OrderAddressCard;