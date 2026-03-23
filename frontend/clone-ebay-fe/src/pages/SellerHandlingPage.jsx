import { useEffect, useMemo, useState } from 'react';
import {
  confirmShipmentHandling,
  getSellerShipments,
  updateShipmentTracking,
} from '../api/orderApi';
import { useToast } from '../components/Toast';
import { formatCurrency } from '../utils/productUtils';
import './SellerHandlingPage.css';

const STATUS_FILTERS = [
  { key: 'ALL', label: 'All shipments' },
  { key: 'PENDING', label: 'Pending handling' },
  { key: 'PICKED_UP', label: 'Picked up' },
  { key: 'IN_TRANSIT', label: 'In transit' },
  { key: 'OUT_FOR_DELIVERY', label: 'Out for delivery' },
  { key: 'DELIVERED', label: 'Delivered' },
  { key: 'CANCELLED', label: 'Cancelled' },
];

function normalizeStatus(value) {
  return String(value || '').trim().toUpperCase();
}

function formatDateTime(value) {
  if (!value) return '--';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '--';

  return date.toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function getStatusBadgeClass(status) {
  switch (normalizeStatus(status)) {
    case 'PENDING':
      return 'seller-handling-badge pending';
    case 'PICKED_UP':
    case 'IN_TRANSIT':
    case 'OUT_FOR_DELIVERY':
      return 'seller-handling-badge moving';
    case 'DELIVERED':
      return 'seller-handling-badge delivered';
    case 'CANCELLED':
      return 'seller-handling-badge cancelled';
    case 'PROCESSING':
      return 'seller-handling-badge processing';
    default:
      return 'seller-handling-badge default';
  }
}

const SellerHandlingPage = () => {
  const { showSuccess, showError } = useToast();

  const [loading, setLoading] = useState(true);
  const [submittingId, setSubmittingId] = useState(null);
  const [shipments, setShipments] = useState([]);
  const [activeFilter, setActiveFilter] = useState('ALL');
  const [refreshKey, setRefreshKey] = useState(0);

  const [handlingForms, setHandlingForms] = useState({});
  const [trackingForms, setTrackingForms] = useState({});

  const fetchShipments = async () => {
    try {
      setLoading(true);

      const data = await getSellerShipments({
        status: activeFilter === 'ALL' ? '' : activeFilter,
        page: 1,
        pageSize: 100,
      });

      setShipments(data?.items || []);
    } catch (error) {
      showError(error.message || 'Failed to load seller shipments');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchShipments();
  }, [activeFilter, refreshKey]);

  const summary = useMemo(() => {
    return {
      total: shipments.length,
      pending: shipments.filter((x) => normalizeStatus(x.shipmentStatus) === 'PENDING').length,
      moving: shipments.filter((x) => {
        const s = normalizeStatus(x.shipmentStatus);
        return s === 'PICKED_UP' || s === 'IN_TRANSIT' || s === 'OUT_FOR_DELIVERY';
      }).length,
      delivered: shipments.filter((x) => normalizeStatus(x.shipmentStatus) === 'DELIVERED').length,
    };
  }, [shipments]);

  const handleHandlingFieldChange = (shipmentId, field, value) => {
    setHandlingForms((prev) => ({
      ...prev,
      [shipmentId]: {
        ...prev[shipmentId],
        [field]: value,
      },
    }));
  };

  const handleTrackingFieldChange = (shipmentId, field, value) => {
    setTrackingForms((prev) => ({
      ...prev,
      [shipmentId]: {
        ...prev[shipmentId],
        [field]: value,
      },
    }));
  };

  const handleConfirmHandling = async (shipment) => {
    const form = handlingForms[shipment.shipmentId] || {};

    try {
      setSubmittingId(`handling-${shipment.shipmentId}`);

      await confirmShipmentHandling(shipment.shipmentId, {
        trackingNumber: form.trackingNumber || undefined,
        note: form.note || 'Seller started packing the order',
        handlingAt: new Date().toISOString(),
      });

      showSuccess(`Shipment #${shipment.shipmentId} is now in seller handling`);
      setRefreshKey((v) => v + 1);
    } catch (error) {
      showError(error.message || 'Failed to confirm handling');
    } finally {
      setSubmittingId(null);
    }
  };

  const handleUpdateTracking = async (shipment, nextStatus) => {
    const form = trackingForms[shipment.shipmentId] || {};

    try {
      setSubmittingId(`${nextStatus}-${shipment.shipmentId}`);

      await updateShipmentTracking(shipment.shipmentId, {
        status: nextStatus,
        trackingNumber: form.trackingNumber || shipment.trackingNumber || undefined,
        description: 'Carrier picked up the package',
        eventTime: new Date().toISOString(),
      });

      showSuccess(`Shipment #${shipment.shipmentId} updated to ${nextStatus}`);
      setRefreshKey((v) => v + 1);
    } catch (error) {
      showError(error.message || 'Failed to update shipment tracking');
    } finally {
      setSubmittingId(null);
    }
  };

  return (
    <div className="seller-handling-page">
      <div className="seller-handling-header">
        <div>
          <p className="seller-handling-eyebrow">Seller operations</p>
          <h1>Seller Handling</h1>
          <p className="seller-handling-subtitle">
            Confirm package preparation, assign tracking numbers, and update shipment movement.
          </p>
        </div>

        <button
          type="button"
          className="seller-handling-refresh"
          onClick={() => setRefreshKey((v) => v + 1)}
        >
          Refresh
        </button>
      </div>

      <div className="seller-handling-summary">
        <div className="seller-handling-summary-card">
          <span>Total</span>
          <strong>{summary.total}</strong>
        </div>
        <div className="seller-handling-summary-card">
          <span>Pending handling</span>
          <strong>{summary.pending}</strong>
        </div>
        <div className="seller-handling-summary-card">
          <span>Moving</span>
          <strong>{summary.moving}</strong>
        </div>
        <div className="seller-handling-summary-card">
          <span>Delivered</span>
          <strong>{summary.delivered}</strong>
        </div>
      </div>

      <div className="seller-handling-filters">
        {STATUS_FILTERS.map((filter) => (
          <button
            key={filter.key}
            type="button"
            className={`seller-handling-filter ${activeFilter === filter.key ? 'active' : ''}`}
            onClick={() => setActiveFilter(filter.key)}
          >
            {filter.label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="seller-handling-empty">Loading seller shipments...</div>
      ) : shipments.length === 0 ? (
        <div className="seller-handling-empty">
          <h3>No shipments found</h3>
          <p>There are no shipments matching this filter yet.</p>
        </div>
      ) : (
        <div className="seller-handling-list">
          {shipments.map((shipment) => {
            const shipmentStatus = normalizeStatus(shipment.shipmentStatus);
            const handlingForm = handlingForms[shipment.shipmentId] || {};
            const trackingForm = trackingForms[shipment.shipmentId] || {};

            const canConfirmHandling = shipmentStatus === 'PENDING';
            const canUpdateTracking = shipmentStatus === 'PENDING';

            return (
              <article className="seller-handling-card" key={shipment.shipmentId}>
                <div className="seller-handling-card-top">
                  <div>
                    <div className="seller-handling-card-title-row">
                      <h3>{shipment.orderCode || `Order #${shipment.orderId}`}</h3>
                      <span className={getStatusBadgeClass(shipment.shipmentStatus)}>
                        {shipment.shipmentStatus || 'PENDING'}
                      </span>
                    </div>
                    <p className="seller-handling-card-meta">
                      Shipment #{shipment.shipmentId} · Buyer: {shipment.buyerName || 'Unknown buyer'}
                    </p>
                  </div>

                  <div className="seller-handling-card-meta-right">
                    <strong>{formatCurrency(shipment.shippingFee || 0)}</strong>
                    <span>{shipment.totalItems || 0} items</span>
                  </div>
                </div>

                <div className="seller-handling-grid">
                  <div className="seller-handling-info-box">
                    <h4>Order info</h4>
                    <p>
                      <span>Order status:</span> <strong>{shipment.orderStatus || '--'}</strong>
                    </p>
                    <p>
                      <span>Order date:</span> <strong>{formatDateTime(shipment.orderDate)}</strong>
                    </p>
                    <p>
                      <span>Tracking number:</span> <strong>{shipment.trackingNumber || '--'}</strong>
                    </p>
                    <p>
                      <span>Estimated delivery:</span>{' '}
                      <strong>{formatDateTime(shipment.estimatedDeliveryDate)}</strong>
                    </p>
                  </div>

                  <div className="seller-handling-info-box">
                    <h4>Destination</h4>
                    <p>{shipment.destinationLabel || '--'}</p>
                  </div>
                </div>

                {!!shipment.items?.length && (
                  <div className="seller-handling-items">
                    <h4>Items</h4>
                    <div className="seller-handling-item-list">
                      {shipment.items.map((item) => (
                        <div className="seller-handling-item" key={item.id}>
                          <div>
                            <strong>{item.productTitle}</strong>
                            <p>Qty: {item.quantity}</p>
                          </div>
                          <span>{formatCurrency(item.lineTotal)}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {canConfirmHandling && (
                  <div className="seller-handling-section">
                    <h4>Step 1: Confirm seller handling</h4>

                    <div className="seller-handling-form-grid">
                      <input
                        type="text"
                        placeholder="Tracking number (optional)"
                        value={handlingForm.trackingNumber || ''}
                        onChange={(e) =>
                          handleHandlingFieldChange(
                            shipment.shipmentId,
                            'trackingNumber',
                            e.target.value
                          )
                        }
                      />
                    </div>

                    <textarea
                      rows="3"
                      placeholder="Handling note"
                      value={handlingForm.note || ''}
                      onChange={(e) =>
                        handleHandlingFieldChange(shipment.shipmentId, 'note', e.target.value)
                      }
                    />

                    <div className="seller-handling-actions">
                      <button
                        type="button"
                        className="seller-handling-primary"
                        disabled={submittingId === `handling-${shipment.shipmentId}`}
                        onClick={() => handleConfirmHandling(shipment)}
                      >
                        {submittingId === `handling-${shipment.shipmentId}`
                          ? 'Confirming...'
                          : 'Confirm handling'}
                      </button>
                    </div>
                  </div>
                )}

                {canUpdateTracking && (
                  <div className="seller-handling-section">
                    <h4>Step 2: Update shipping progress</h4>

                    <div className="seller-handling-form-grid">
                      <input
                        type="text"
                        placeholder="Tracking number"
                        value={trackingForm.trackingNumber || shipment.trackingNumber || ''}
                        onChange={(e) =>
                          handleTrackingFieldChange(
                            shipment.shipmentId,
                            'trackingNumber',
                            e.target.value
                          )
                        }
                      />
                    </div>

                    <div className="seller-handling-actions">
                      <button
                        type="button"
                        className="seller-handling-secondary"
                        disabled={submittingId === `PICKED_UP-${shipment.shipmentId}`}
                        onClick={() => handleUpdateTracking(shipment, 'PICKED_UP')}
                      >
                        {submittingId === `PICKED_UP-${shipment.shipmentId}`
                          ? 'Updating...'
                          : 'Mark as picked up'}
                      </button>
                    </div>
                  </div>
                )}
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default SellerHandlingPage;