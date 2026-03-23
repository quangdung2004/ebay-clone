import { useParams, Link, useLocation } from 'react-router-dom';
import { useOrderDetail } from '../hooks/useOrderDetail';
import { formatDateTime } from '../utils/productUtils';
import TrackingTimeline from '../components/orders/TrackingTimeline';
import TrackingMap from '../components/orders/TrackingMap';
import './OrderTrackingPage.css';

const OrderTrackingPage = () => {
  const { id } = useParams();
  const location = useLocation();
  const trySellerFirst = location.state?.trySellerFirst || false;
  const { order, loading, error } = useOrderDetail(id, trySellerFirst);

  if (loading) {
    return <div className="tracking-page-container"><div className="tracking-loading">Loading tracking info...</div></div>;
  }

  if (error || !order) {
    return (
      <div className="tracking-page-container">
        <div className="tracking-error">
          <h3>Failed to load tracking details</h3>
          <p>{error?.message || 'Order tracking not found.'}</p>
          <Link to={`/orders/${id}`} className="btn-back">Back to Order</Link>
        </div>
      </div>
    );
  }

  const shipping = order.shippings && order.shippings.length > 0 ? order.shippings[0] : null;

  if (!shipping) {
    return (
      <div className="tracking-page-container">
        <div className="tracking-empty">
          <h2>Order O-#{order.id} Tracking</h2>
          <p>No shipment has been created for this order yet.</p>
          <Link to={`/orders/${id}`} className="btn-back">Back to Order</Link>
        </div>
      </div>
    );
  }

  const trackingEvents = shipping.events || [];
  const hasMapData = trackingEvents.some(e => e.latitude != null && e.longitude != null);
  const hasDestination = !!order.address;
  const showMap = hasMapData || hasDestination;

  return (
    <div className="tracking-page-container">
      <div className="tracking-header">
        <Link to={`/orders/${id}`} className="btn-back-link">← Back to Order</Link>
        <h1>Shipment Tracking</h1>
        <p className="tracking-subtitle">For Order O-#{order.id}</p>
      </div>

      <div className="tracking-summary-card">
        <div className="summary-grid">
           <div className="summary-col">
              <span className="summary-label">Tracking Number</span>
              <strong className="summary-value tracking-number">{shipping.trackingNumber || 'N/A'}</strong>
           </div>
           <div className="summary-col">
              <span className="summary-label">Carrier</span>
              <strong className="summary-value">{shipping.carrier || 'N/A'}</strong>
           </div>
           <div className="summary-col">
              <span className="summary-label">Status</span>
              <strong className={`summary-value status-${shipping.status?.toLowerCase()}`}>{shipping.status || 'N/A'}</strong>
           </div>
           <div className="summary-col">
              <span className="summary-label">Estimated Arrival</span>
              <strong className="summary-value">
                {shipping.estimatedArrival ? formatDateTime(shipping.estimatedArrival) : 'Pending'}
              </strong>
           </div>
        </div>

        <div className="summary-meta">
           <p><strong>Provider:</strong> {shipping.provider || 'N/A'}</p>
           <p><strong>Shipped At:</strong> {shipping.shippedAt ? formatDateTime(shipping.shippedAt) : '--'}</p>
           {shipping.deliveredAt && <p><strong>Delivered At:</strong> {formatDateTime(shipping.deliveredAt)}</p>}
        </div>
      </div>

      <div className="tracking-timeline-section">
          {showMap ? (
             <div className="tracking-history-grid" style={{ display: 'flex', gap: '2rem', flexWrap: 'wrap' }}>
                 <div className="tracking-map-wrapper" style={{ flex: '2 1 500px' }}>
                     <h2>Live Map</h2>
                     <TrackingMap
                       events={trackingEvents}
                       destinationAddress={order.address}
                     />
                 </div>
                 <div className="tracking-list-wrapper" style={{ flex: '1 1 300px' }}>
                    <h2>Tracking History</h2>
                    <TrackingTimeline events={trackingEvents} />
                 </div>
             </div>
          ) : (
             <>
               <h2>Tracking History</h2>
               <TrackingTimeline events={trackingEvents} />
             </>
          )}
      </div>
    </div>
  );
};

export default OrderTrackingPage;
