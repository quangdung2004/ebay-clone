import { formatDateTime } from '../../utils/productUtils';

const OrderAddressCard = ({ address, canChange, onChangeClick, changeCount = 0, lastChangedAt }) => {
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
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
         <h3 className="order-section-title" style={{ margin: 0 }}>Shipping Address</h3>
         {canChange && (
           <button 
             onClick={onChangeClick}
             className="btn-change-address"
             style={{ fontSize: '0.85rem', color: 'var(--primary-color)', background: 'none', border: '1px solid var(--primary-color)', padding: '2px 8px', borderRadius: '4px', cursor: 'pointer' }}
           >
             Change
           </button>
         )}
      </div>

      <div className="order-address-box">
        <p className="order-address-name">
          <strong>{address.fullName}</strong>
          {address.phone && <span className="order-address-phone" style={{ marginLeft: '10px', color: '#666', fontSize: '0.9rem' }}>{address.phone}</span>}
        </p>
        <p className="order-address-line">{address.street}</p>
        <p className="order-address-line">
          {address.city}, {address.state}
        </p>
        <p className="order-address-line">{address.country}</p>
      </div>

      {changeCount > 0 && (
        <div style={{ marginTop: '1rem', paddingTop: '0.75rem', borderTop: '1px solid #f3f4f6', fontSize: '0.8rem', color: '#6b7280' }}>
          <p style={{ margin: 0 }}>
             Address has been changed <strong>{changeCount}</strong> time(s).
             {lastChangedAt && <span> Last updated: {formatDateTime(lastChangedAt)}</span>}
          </p>
        </div>
      )}
    </div>
  );
};

export default OrderAddressCard;
