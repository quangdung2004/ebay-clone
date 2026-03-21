import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { getMyAddresses } from '../api/addressApi';
import { checkoutOrder } from '../api/orderApi';
import { useToast } from '../components/Toast';
import { formatCurrency, getPlaceholderImage, normalizeProductImageUrl } from '../utils/productUtils';
import { removeFromCart } from '../utils/cartUtils';
import {
  DEFAULT_ORIGIN_ADDRESS,
  DEFAULT_ORIGIN_POINT,
  calculateShippingEstimate,
  formatAddressLabel,
  formatCoordinates,
  formatDistance,
  formatDuration,
  resolvePoint,
  getDrivingRoute,
} from '../utils/shippingMap';
import './CheckoutPage.css';

const CHECKOUT_ADDRESS_KEY = 'selectedShippingAddressId';

const getStoredSelectedAddressId = () => {
  const val = localStorage.getItem(CHECKOUT_ADDRESS_KEY);
  return val ? Number(val) : null;
};

const storeSelectedAddressId = (id) => localStorage.setItem(CHECKOUT_ADDRESS_KEY, String(id));

const CheckoutPage = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();

  const [items, setItems] = useState([]);
  const [addresses, setAddresses] = useState([]);
  const [selectedAddressId, setSelectedAddressId] = useState(null);
  const [paymentMethod, setPaymentMethod] = useState('COD');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [shippingPreview, setShippingPreview] = useState({
    loading: false,
    error: '',
    distanceKm: 0,
    durationMinutes: 0,
    estimatedFee: 0,
  });

  useEffect(() => {
    if (!location.state || !location.state.items || location.state.items.length === 0) {
      showError('No items found for checkout.');
      navigate('/cart');
      return;
    }

    setItems(location.state.items);

    const loadData = async () => {
      try {
        const addrData = await getMyAddresses();
        setAddresses(addrData || []);

        if (addrData && addrData.length > 0) {
          const storedId = getStoredSelectedAddressId();
          let chosen = addrData.find((a) => a.id === storedId);
          if (!chosen) chosen = addrData.find((a) => a.isDefault);
          if (!chosen) chosen = addrData[0];
          setSelectedAddressId(chosen.id);
        }
      } catch (err) {
        showError('Failed to load addresses');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [location.state, navigate, showError]);

  const selectedAddress = useMemo(
    () => addresses.find((addr) => Number(addr.id) === Number(selectedAddressId)) || null,
    [addresses, selectedAddressId]
  );

  useEffect(() => {
    let isCancelled = false;

    const loadShippingPreview = async () => {
      if (!selectedAddress) {
        setShippingPreview({
          loading: false,
          error: 'Please select an address to estimate shipping.',
          distanceKm: 0,
          durationMinutes: 0,
          estimatedFee: 0,
        });
        return;
      }

      try {
        setShippingPreview((prev) => ({ ...prev, loading: true, error: '' }));

        const [originPoint, destinationPoint] = await Promise.all([
  DEFAULT_ORIGIN_POINT
    ? Promise.resolve(DEFAULT_ORIGIN_POINT)
    : resolvePoint(DEFAULT_ORIGIN_ADDRESS, DEFAULT_ORIGIN_ADDRESS),
  resolvePoint(selectedAddress, formatAddressLabel(selectedAddress)),
]);

        const route = await getDrivingRoute(originPoint, destinationPoint);
        const estimatedFee = calculateShippingEstimate(route.distanceKm, items.length || 1);

        if (isCancelled) return;

        setShippingPreview({
          loading: false,
          error: '',
          distanceKm: route.distanceKm,
          durationMinutes: route.durationMinutes,
          estimatedFee,
        });
      } catch (previewError) {
        if (isCancelled) return;

        setShippingPreview({
          loading: false,
          error: previewError.message || 'Unable to calculate distance-based shipping.',
          distanceKm: 0,
          durationMinutes: 0,
          estimatedFee: 0,
        });
      }
    };

    loadShippingPreview();

    return () => {
      isCancelled = true;
    };
  }, [items.length, selectedAddress]);

  const itemSubtotal = useMemo(() => {
    return items.reduce((sum, item) => {
      return sum + Number(item.product?.price || item.price || 0) * Number(item.quantity || 0);
    }, 0);
  }, [items]);

  const itemCount = useMemo(
    () => items.reduce((sum, item) => sum + Number(item.quantity || 0), 0),
    [items]
  );

  const estimatedTotal = itemSubtotal + Number(shippingPreview.estimatedFee || 0);

  const handleCheckout = async () => {
    if (!selectedAddressId) {
      showError('Please select a shipping address.');
      return;
    }

    try {
      setSubmitting(true);
      const payload = {
        paymentMethod,
        addressId: selectedAddressId,
        items: items.map((item) => ({
          productId: Number(item.productId),
          quantity: Number(item.quantity),
        })),
      };

      const result = await checkoutOrder(payload);

      items.forEach((item) => removeFromCart(item.productId));

      if (paymentMethod === 'PAYPAL' && result?.id) {
        showSuccess('Order created. Please complete your PayPal payment.');
        navigate(`/orders/${result.id}`);
        return;
      }

      if (result?.id) {
        showSuccess('Order created successfully!');
        navigate(`/orders/${result.id}`);
        return;
      }

      showSuccess('Order created successfully!');
      navigate('/orders');
    } catch (err) {
      showError(err.message || 'Checkout failed');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="checkout-loading">Loading checkout details...</div>;

  return (
    <div className="checkout-page">
      <div className="checkout-container">
        <div className="checkout-main">
          <section className="checkout-section">
            <div className="checkout-section-header">
              <h2>1. Shipping Address</h2>
              {addresses.length === 0 ? (
                <Link to="/addresses?redirect=/checkout" className="btn-add-address">Add Address</Link>
              ) : (
                <Link to="/addresses?redirect=/checkout" className="link-edit-address">Manage</Link>
              )}
            </div>

            <div className="checkout-address-list">
              {addresses.map((addr) => (
                <label key={addr.id} className={`checkout-address-card ${selectedAddressId === addr.id ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="address"
                    value={addr.id}
                    checked={selectedAddressId === addr.id}
                    onChange={() => {
                      setSelectedAddressId(addr.id);
                      storeSelectedAddressId(addr.id);
                    }}
                  />
                  <div className="checkout-address-content">
                    <strong>
                      {addr.fullName}{' '}
                      <span className="checkout-address-phone">{addr.phone}</span>
                    </strong>
                    <p>{addr.street}, {addr.city}, {addr.state}, {addr.country}</p>
                    {(addr.latitude != null && addr.longitude != null) && (
                      <p>{formatCoordinates(addr.latitude, addr.longitude)}</p>
                    )}
                    {addr.isDefault && <span className="badge-default-address">Default</span>}
                  </div>
                </label>
              ))}
              {addresses.length === 0 && (
                <div className="checkout-no-address">
                  You don't have any shipping address. Please add one to continue.
                </div>
              )}
            </div>

            <div className="checkout-distance-card">
              <div className="checkout-distance-header">
                <h3>Distance-based shipping preview</h3>
                <span>Origin: {DEFAULT_ORIGIN_ADDRESS}</span>
              </div>

              {shippingPreview.loading ? (
                <p className="checkout-distance-muted">Calculating driving distance and shipping fee…</p>
              ) : shippingPreview.error ? (
                <p className="checkout-distance-error">{shippingPreview.error}</p>
              ) : (
                <div className="checkout-distance-grid">
                  <div>
                    <span className="checkout-distance-label">Distance</span>
                    <strong>{formatDistance(shippingPreview.distanceKm)}</strong>
                  </div>
                  <div>
                    <span className="checkout-distance-label">Driving time</span>
                    <strong>{formatDuration(shippingPreview.durationMinutes)}</strong>
                  </div>
                  <div>
                    <span className="checkout-distance-label">Estimated shipping fee</span>
                    <strong>{formatCurrency(shippingPreview.estimatedFee)}</strong>
                  </div>
                </div>
              )}

              <p className="checkout-summary-note">
                The frontend shows an estimated shipping fee based on real driving distance. The order total shown here uses the estimated shipping fee.
              </p>
            </div>
          </section>

          <section className="checkout-section">
            <div className="checkout-section-header">
              <h2>2. Payment Method</h2>
            </div>
            <div className="checkout-payment-methods">
              <label className={`checkout-payment-card ${paymentMethod === 'COD' ? 'selected' : ''}`}>
                <input
                  type="radio"
                  name="payment"
                  value="COD"
                  checked={paymentMethod === 'COD'}
                  onChange={() => setPaymentMethod('COD')}
                />
                <div className="payment-content">
                  <div className="payment-icon">💵</div>
                  <span>Cash on Delivery (COD)</span>
                </div>
              </label>

              <label className={`checkout-payment-card ${paymentMethod === 'PAYPAL' ? 'selected' : ''}`}>
                <input
                  type="radio"
                  name="payment"
                  value="PAYPAL"
                  checked={paymentMethod === 'PAYPAL'}
                  onChange={() => setPaymentMethod('PAYPAL')}
                />
                <div className="payment-content">
                  <div className="payment-icon">🔵</div>
                  <span>PayPal</span>
                </div>
              </label>
            </div>
          </section>

          <section className="checkout-section">
            <div className="checkout-section-header">
              <h2>3. Order Items</h2>
            </div>
            <div className="checkout-items-list">
              {items.map((item) => {
                const product = item.product;
                const image = normalizeProductImageUrl(product?.images?.[0] || item.image);
                const price = Number(product?.price || item.price || 0);

                return (
                  <div className="checkout-item-row" key={item.productId}>
                    <img src={image || getPlaceholderImage()} alt={product?.title || item.title} className="checkout-item-image" />
                    <div className="checkout-item-details">
                      <h4>{product?.title || item.title}</h4>
                      <div className="checkout-item-meta">
                        <span>Qty: {item.quantity}</span>
                        <span>Price: {formatCurrency(price)}</span>
                      </div>
                    </div>
                    <div className="checkout-item-total">
                      <strong>{formatCurrency(price * Number(item.quantity))}</strong>
                    </div>
                  </div>
                );
              })}
            </div>
          </section>
        </div>

        <aside className="checkout-sidebar">
          <div className="checkout-summary-card">
            <h3>Order Summary</h3>
            <div className="summary-row">
              <span>Items ({itemCount}):</span>
              <span>{formatCurrency(itemSubtotal)}</span>
            </div>
            <div className="summary-row">
              <span>Shipping:</span>
              <span>
                {shippingPreview.loading
                  ? 'Calculating...'
                  : shippingPreview.error
                    ? 'Unavailable'
                    : formatCurrency(shippingPreview.estimatedFee)}
              </span>
            </div>
            <p className="checkout-summary-note">
              Estimated from driving distance to the selected shipping address.
            </p>
            <hr />
            <div className="summary-row total">
              <strong>Estimated total:</strong>
              <strong>{formatCurrency(estimatedTotal)}</strong>
            </div>

            <button
              className="btn-place-order"
              onClick={handleCheckout}
              disabled={submitting || addresses.length === 0 || !selectedAddressId}
            >
              {submitting ? 'Processing...' : paymentMethod === 'PAYPAL' ? 'Create Order & Pay' : 'Place Order'}
            </button>
            <p className="checkout-terms">
              By placing your order, you agree to our Terms of Use and Privacy Policy.
            </p>
          </div>
        </aside>
      </div>
    </div>
  );
};

export default CheckoutPage;
