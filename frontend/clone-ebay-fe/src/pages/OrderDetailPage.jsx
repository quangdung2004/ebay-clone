import { Link, useParams } from 'react-router-dom';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useOrderDetail } from '../hooks/useOrderDetail';
import { useOrderTracking } from '../hooks/useOrderTracking';
import OrderStatusBadge from '../components/orders/OrderStatusBadge';
import OrderItemList from '../components/orders/OrderItemList';
import OrderAddressCard from '../components/orders/OrderAddressCard';
import PaymentHistoryCard from '../components/orders/PaymentHistoryCard';
import ShippingInfoCard from '../components/orders/ShippingInfoCard';
import OrderTrackingPanel from '../components/orders/OrderTrackingPanel';
import PayPalButtonsSection from '../components/payments/PayPalButtonsSection';
import OrderActionBar from '../components/orders/OrderActionBar';
import { getMyAddresses } from '../api/addressApi';
import { updateOrderAddress } from '../api/orderApi';
import { useToast } from '../components/Toast';
import { formatCurrency } from '../utils/productUtils';
import '../components/orders/OrderTrackingPanel.css';
import './OrderDetailPage.css';

const formatOrderDate = (value) => {
  if (!value) return '--';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '--'
    : date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      });
};

const OrderDetailPage = () => {
  const { id } = useParams();
  const { order, loading, error, refreshOrder } = useOrderDetail(id);
  const {
    tracking,
    loading: trackingLoading,
    error: trackingError,
    refreshTracking,
  } = useOrderTracking(id);

  const { showSuccess, showError } = useToast();

  const [addresses, setAddresses] = useState([]);
  const [selectedAddressId, setSelectedAddressId] = useState('');
  const [showAddressEditor, setShowAddressEditor] = useState(false);
  const [addressLoading, setAddressLoading] = useState(false);
  const [isUpdatingAddress, setIsUpdatingAddress] = useState(false);
  const [estimatedShippingFee, setEstimatedShippingFee] = useState(0);

  const handleEstimatedShippingChange = useCallback((value) => {
    setEstimatedShippingFee(Number(value || 0));
  }, []);

  const handlePaymentSuccess = async () => {
    await Promise.all([refreshOrder(), refreshTracking()]);
  };

  const availableAddresses = useMemo(
    () => (addresses || []).filter((item) => item.id !== order?.address?.id),
    [addresses, order?.address?.id]
  );

  useEffect(() => {
    if (!order?.canUpdateAddress) {
      setShowAddressEditor(false);
      return;
    }

    let ignore = false;

    const loadAddresses = async () => {
      try {
        setAddressLoading(true);
        const data = await getMyAddresses();
        if (ignore) return;

        setAddresses(data || []);

        const firstAvailable = (data || []).find(
          (item) => item.id !== order?.address?.id
        );
        setSelectedAddressId(firstAvailable ? String(firstAvailable.id) : '');
      } catch (err) {
        if (!ignore) {
          showError(err.message || 'Failed to load addresses');
        }
      } finally {
        if (!ignore) {
          setAddressLoading(false);
        }
      }
    };

    loadAddresses();

    return () => {
      ignore = true;
    };
  }, [order?.canUpdateAddress, order?.address?.id, showError]);

  const handleUpdateAddress = async () => {
    if (!selectedAddressId) {
      showError('Please choose another address');
      return;
    }

    try {
      setIsUpdatingAddress(true);
      await updateOrderAddress(order.id, Number(selectedAddressId));
      await Promise.all([refreshOrder(), refreshTracking()]);
      setShowAddressEditor(false);
      showSuccess('Shipping address updated successfully');
    } catch (err) {
      showError(err.message || 'Failed to update shipping address');
    } finally {
      setIsUpdatingAddress(false);
    }
  };

  if (loading) {
    return (
      <div className="order-detail-page">
        <div className="order-detail-loading">Loading order details...</div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="order-detail-page">
        <div className="order-detail-error">
          <h3>Oops! Order not found.</h3>
          <p>{error?.message || 'We could not find the order details.'}</p>
          <Link to="/orders" className="btn-back">
            Back to Orders
          </Link>
        </div>
      </div>
    );
  }

  const itemSubtotal = Number(order.itemSubtotal || 0);
  const discountTotal = Number(order.discountTotal || 0);
  const taxTotal = Number(order.taxTotal || 0);
  const displayShippingTotal = Number(estimatedShippingFee || 0);
  const grandTotal = itemSubtotal + displayShippingTotal;

  return (
    <div className="order-detail-page">
      <div className="order-detail-header-wrap">
        <Link to="/orders" className="back-link">
          ← Back to orders
        </Link>

        <div className="order-detail-header">
          <div className="order-title">
           <h1>{order.orderCode || `Order #${order.id}`}</h1>
            <OrderStatusBadge status={order.status} />
          </div>
          <p className="order-date">Placed on {formatOrderDate(order.orderDate)}</p>
        </div>
      </div>

      <div className="order-detail-grid">
        <div className="order-detail-main">
          <PayPalButtonsSection
            order={order}
            onPaymentSuccess={handlePaymentSuccess}
          />

          <OrderTrackingPanel
            order={order}
            tracking={tracking}
            loading={trackingLoading}
            error={trackingError}
            onEstimatedShippingChange={handleEstimatedShippingChange}
          />

          <OrderItemList items={order.items || []} />
          <OrderActionBar order={order} onOrderCancelled={refreshOrder} />
        </div>

        <div className="order-detail-sidebar">
          <div className="order-card-panel">
            <h3 className="order-section-title">Order Summary</h3>

            <div className="order-summary-row">
              <span>Items subtotal</span>
              <strong>{formatCurrency(itemSubtotal)}</strong>
            </div>

            <div className="order-summary-row">
              <span>Shipping</span>
              <strong>{formatCurrency(displayShippingTotal)}</strong>
            </div>

            <div className="order-summary-row">
              <span>Tax</span>
              <strong>{formatCurrency(taxTotal)}</strong>
            </div>

            <div className="order-summary-row">
              <span>Discount</span>
              <strong>-{formatCurrency(discountTotal)}</strong>
            </div>

            <div className="order-summary-row order-summary-row-total">
              <span>Total</span>
              <strong>{formatCurrency(grandTotal)}</strong>
            </div>
          </div>

          <OrderAddressCard
            address={order.address}
            canUpdateAddress={order.canUpdateAddress}
            remainingAddressChanges={order.remainingAddressChanges}
            onEditAddress={() => setShowAddressEditor((prev) => !prev)}
            isUpdatingAddress={isUpdatingAddress}
          />

          {showAddressEditor && order.canUpdateAddress && (
            <div className="order-card-panel">
              <h3 className="order-section-title">Update shipping address</h3>

              {addressLoading ? (
                <p className="order-text-muted">Loading your saved addresses...</p>
              ) : availableAddresses.length ? (
                <>
                  <select
                    className="order-address-select"
                    value={selectedAddressId}
                    onChange={(event) => setSelectedAddressId(event.target.value)}
                    disabled={isUpdatingAddress}
                  >
                    {availableAddresses.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.fullName} - {item.street}, {item.city}, {item.state}, {item.country}
                      </option>
                    ))}
                  </select>

                  <div className="order-address-actions">
                    <button
                      type="button"
                      className="btn-order-address-save"
                      onClick={handleUpdateAddress}
                      disabled={isUpdatingAddress || !selectedAddressId}
                    >
                      {isUpdatingAddress ? 'Saving...' : 'Save new address'}
                    </button>
                  </div>
                </>
              ) : (
                <p className="order-text-muted">
                  You need another saved address before changing this order.
                </p>
              )}
            </div>
          )}

          <PaymentHistoryCard payments={order.payments || []} />
          <ShippingInfoCard shipments={order.shipments || []} />
        </div>
      </div>
    </div>
  );
};

export default OrderDetailPage;