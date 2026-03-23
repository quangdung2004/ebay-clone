import { Link } from 'react-router-dom';
import { formatDateTime } from '../../utils/productUtils';
import TrackingTimeline from './TrackingTimeline';

const ShippingInfoCard = ({ shippings, orderId, orderStatus }) => {
  if (!shippings || shippings.length === 0) {
    let emptyMessage = "Seller has not created a shipment yet.";
    if (orderStatus === 'PENDING_PAYMENT') emptyMessage = "Awaiting payment before shipping.";
    else if (orderStatus === 'PAID') emptyMessage = "Order is paid. Awaiting seller to start processing.";
    else if (orderStatus === 'PROCESSING') emptyMessage = "Seller is currently processing the shipment.";

    return (
      <div className="order-card-panel">
        <h3 className="order-section-title">Shipping & Tracking</h3>
        <p className="order-text-muted" style={{fontStyle: 'italic', color: '#6b7280', padding: '1rem', background: '#f9fafb', borderRadius: '4px'}}>
            {emptyMessage}
        </p>
      </div>
    );
  }

  return (
    <div className="order-card-panel" style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      <h3 className="order-section-title">Shipping & Tracking</h3>
      
      {shippings.map((s) => (
        <div key={s.id} style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
          {/* Main Info Box */}
          <div className="shipping-info-item" style={{ background: '#f8fafc', padding: '1.25rem', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1rem', paddingBottom: '1rem', borderBottom: '1px solid #e2e8f0' }}>
               <div>
                  <span style={{ fontSize: '0.85rem', color: '#64748b', textTransform: 'uppercase' }}>Tracking Number</span>
                  <div style={{ fontSize: '1.1rem', fontWeight: 'bold', fontFamily: 'monospace', color: 'var(--primary-color)' }}>{s.trackingNumber || '--'}</div>
               </div>
               <div style={{ textAlign: 'right' }}>
                  <span style={{ fontSize: '0.85rem', color: '#64748b', textTransform: 'uppercase' }}>Status</span>
                  <div>
                    <span style={{ 
                        display: 'inline-block', 
                        padding: '0.25rem 0.75rem', 
                        borderRadius: '9999px',
                        fontSize: '0.85rem',
                        fontWeight: '600',
                        backgroundColor: s.status === 'DELIVERED' ? '#dcfce7' : '#dbeafe',
                        color: s.status === 'DELIVERED' ? '#166534' : '#1e40af'
                    }}>
                        {s.status || 'Pending update'}
                    </span>
                  </div>
               </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) minmax(0, 1fr)', gap: '1rem' }}>
               <div>
                 <p style={{ margin: '0 0 0.25rem 0', fontSize: '0.85rem', color: '#64748b' }}>Carrier / Provider</p>
                 <strong style={{ fontSize: '0.95rem', color: '#334155' }}>{s.carrier || '--'} {s.provider ? `(${s.provider})` : ''}</strong>
               </div>
               <div>
                 <p style={{ margin: '0 0 0.25rem 0', fontSize: '0.85rem', color: '#64748b' }}>Est. Arrival</p>
                 <strong style={{ fontSize: '0.95rem', color: '#334155' }}>{s.estimatedArrival ? new Date(s.estimatedArrival).toLocaleDateString() : '--'}</strong>
               </div>
               <div>
                 <p style={{ margin: '0 0 0.25rem 0', fontSize: '0.85rem', color: '#64748b' }}>Shipped At</p>
                 <strong style={{ fontSize: '0.95rem', color: '#334155' }}>{s.shippedAt ? formatDateTime(s.shippedAt) : '--'}</strong>
               </div>
               <div>
                 <p style={{ margin: '0 0 0.25rem 0', fontSize: '0.85rem', color: '#64748b' }}>Delivered At</p>
                 <strong style={{ fontSize: '0.95rem', color: '#334155' }}>{s.deliveredAt ? formatDateTime(s.deliveredAt) : '--'}</strong>
               </div>
            </div>

            {s.lastCheckpoint && (
                <div style={{ marginTop: '1rem', paddingTop: '1rem', borderTop: '1px dashed #cbd5e1' }}>
                  <p style={{ margin: '0 0 0.25rem 0', fontSize: '0.85rem', color: '#64748b' }}>Last Checkpoint</p>
                  <p style={{ margin: 0, fontSize: '0.95rem', color: '#334155' }}>
                      {s.lastCheckpoint} <br />
                      <span style={{ fontSize: '0.85rem', color: '#94a3b8' }}>{s.lastCheckpointTime ? formatDateTime(s.lastCheckpointTime) : ''}</span>
                  </p>
                </div>
            )}

            {orderId && (
              <div style={{ marginTop: '1.25rem' }}>
                <Link to={`/orders/${orderId}/tracking`} className="btn-track-package">
                  Open Detailed Tracking Page →
                </Link>
              </div>
            )}
          </div>

          {/* Inline Tracking Timeline */}
          <div>
              <h4 style={{ margin: '0 0 1rem 0', fontSize: '1rem', color: '#334155' }}>Tracking Timeline</h4>
              <div style={{ background: '#fff', border: '1px solid #e2e8f0', padding: '1rem', borderRadius: '8px' }}>
                  <TrackingTimeline events={s.events || []} />
              </div>
          </div>
        </div>
      ))}
    </div>
  );
};

export default ShippingInfoCard;
