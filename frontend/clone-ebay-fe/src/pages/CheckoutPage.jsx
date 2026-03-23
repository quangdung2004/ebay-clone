import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getProductById } from '../api/productApi';
import { getMyAddresses } from '../api/addressApi';
import { checkoutOrder, previewCheckoutOrder, getMyCoupons } from '../api/orderApi';
import { parseApiError } from '../utils/errorUtils';
import {
  clearCart,
  getCart,
  removeFromCart,
} from '../utils/cartUtils';
import { formatCurrency } from '../utils/format';
import { getPlaceholderImage, normalizeProductImageUrl } from '../utils/image';
import { useToast } from '../components/Toast';
import './CheckoutPage.css';

const DEFAULT_PAYMENT_METHOD = 'PAYPAL';
const STORAGE_KEY_SELECTED_ADDRESS = 'checkout_selected_address_id';

const getStoredSelectedAddressId = () => {
  const raw = localStorage.getItem(STORAGE_KEY_SELECTED_ADDRESS);
  return raw ? Number(raw) : null;
};

const storeSelectedAddressId = (id) => {
  if (!id) return;
  localStorage.setItem(STORAGE_KEY_SELECTED_ADDRESS, String(id));
};

const CheckoutPage = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();

  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [pricingLoading, setPricingLoading] = useState(false);

  const [items, setItems] = useState([]);
  const [addresses, setAddresses] = useState([]);
  const [selectedAddressId, setSelectedAddressId] = useState(null);

  const [availableCoupons, setAvailableCoupons] = useState([]);
  const [selectedCouponCode, setSelectedCouponCode] = useState('');

  const [pricing, setPricing] = useState(null);

  useEffect(() => {
    const cart = getCart();

    if (!cart || cart.length === 0) {
      showError('Your cart is empty.');
      navigate('/cart');
      return;
    }

    const loadData = async () => {
      try {
        const cartDetails = await Promise.all(
          cart.map(async (item) => {
            try {
              const product = await getProductById(item.productId);
              return {
                ...item,
                product,
              };
            } catch {
              return {
                ...item,
                product: null,
              };
            }
          })
        );

        const validItems = cartDetails.filter((x) => x.product);
        if (validItems.length === 0) {
          clearCart();
          showError('No valid products found in cart.');
          navigate('/cart');
          return;
        }

        setItems(validItems);

        const [addrData, couponData] = await Promise.all([
          getMyAddresses(),
          getMyCoupons(),
        ]);

        setAddresses(addrData || []);
        setAvailableCoupons(couponData || []);

        if (addrData && addrData.length > 0) {
          const storedId = getStoredSelectedAddressId();
          let chosen = addrData.find((a) => a.id === storedId);

          if (!chosen) chosen = addrData.find((a) => a.isDefault);
          if (!chosen) chosen = addrData[0];

          if (chosen) {
            setSelectedAddressId(chosen.id);
            storeSelectedAddressId(chosen.id);
          }
        }
      } catch {
        showError('Failed to load checkout data.');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [navigate, showError]);

  const localSubtotal = useMemo(
    () => items.reduce((sum, item) => sum + Number(item.quantity) * Number(item.product?.price || item.price || 0), 0),
    [items]
  );

  const itemCount = useMemo(
    () => items.reduce((sum, item) => sum + Number(item.quantity), 0),
    [items]
  );

  useEffect(() => {
    if (!selectedAddressId || items.length === 0) {
      setPricing(null);
      return;
    }

    let ignore = false;

    const loadPricing = async () => {
      try {
        setPricingLoading(true);

        const result = await previewCheckoutOrder({
          paymentMethod: DEFAULT_PAYMENT_METHOD,
          addressId: selectedAddressId,
          couponCode: selectedCouponCode || null,
          items: items.map((i) => ({
            productId: Number(i.productId),
            quantity: Number(i.quantity),
          })),
        });

        if (!ignore) {
          setPricing(result);
        }
      } catch (err) {
        if (!ignore) {
          const parsed = parseApiError(err, 'Failed to calculate order total.');
          setPricing(null);
          showError(parsed.message);
        }
      } finally {
        if (!ignore) {
          setPricingLoading(false);
        }
      }
    };

    loadPricing();

    return () => {
      ignore = true;
    };
  }, [selectedAddressId, items, selectedCouponCode, showError]);

  const handleCouponChange = (value) => {
    setSelectedCouponCode(value);
  };

  const handleCheckout = async () => {
    if (!selectedAddressId) {
      showError('Please select a shipping address.');
      return;
    }

    try {
      setSubmitting(true);

      await checkoutOrder({
        paymentMethod: DEFAULT_PAYMENT_METHOD,
        addressId: selectedAddressId,
        couponCode: selectedCouponCode || null,
        items: items.map((i) => ({
          productId: Number(i.productId),
          quantity: Number(i.quantity),
        })),
      });

      items.forEach((i) => removeFromCart(i.productId));
      showSuccess('Order created successfully.');
      navigate('/orders');
    } catch (err) {
      const parsed = parseApiError(err, 'Checkout failed.');
      showError(parsed.message);
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
                <Link to="/addresses?redirect=/checkout" className="btn-add-address">
                  Add Address
                </Link>
              ) : (
                <Link to="/addresses?redirect=/checkout" className="link-edit-address">
                  Manage
                </Link>
              )}
            </div>

            <div className="checkout-address-list">
              {addresses.map((addr) => (
                <label
                  key={addr.id}
                  className={`checkout-address-card ${selectedAddressId === addr.id ? 'selected' : ''}`}
                >
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
                      {addr.fullName} <span className="checkout-address-phone">{addr.phone}</span>
                    </strong>
                    <p>
                      {addr.street}, {addr.city}, {addr.state}, {addr.country}
                    </p>
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
          </section>

          <section className="checkout-section">
            <div className="checkout-section-header">
              <h2>2. Payment Method</h2>
            </div>

            <div className="checkout-payment-methods">
              <label className="checkout-payment-card selected">
                <input type="radio" name="payment" value="PAYPAL" checked readOnly />
                <div className="payment-content">
                  <div className="payment-icon">🔵</div>
                  <span>PayPal</span>
                </div>
              </label>
            </div>
          </section>

          <section className="checkout-section">
            <div className="checkout-section-header">
              <h2>3. Discount Coupon</h2>
            </div>

            <select
              value={selectedCouponCode}
              onChange={(e) => handleCouponChange(e.target.value)}
              disabled={pricingLoading}
              style={{
                width: '100%',
                padding: '12px',
                borderRadius: '8px',
                border: '1px solid #d0d7de',
                background: '#fff',
              }}
            >
              <option value="">Do not use coupon</option>
              {availableCoupons.map((coupon) => (
                <option key={coupon.userCouponId} value={coupon.code}>
                  {coupon.code} - {coupon.discountPercent}% off - Qty: {coupon.quantity}
                </option>
              ))}
            </select>

            {selectedCouponCode && (
              <p style={{ marginTop: '10px', color: '#0a7a2f', fontWeight: 600 }}>
                Selected coupon: {selectedCouponCode}
              </p>
            )}

            {availableCoupons.length === 0 && (
              <p style={{ marginTop: '10px', color: '#6b7280' }}>
                You do not have any available coupons.
              </p>
            )}
          </section>

          <section className="checkout-section">
            <div className="checkout-section-header">
              <h2>4. Order Items</h2>
            </div>

            <div className="checkout-items-list">
              {items.map((item) => {
                const product = item.product;
                const image = normalizeProductImageUrl(product?.images?.[0] || item.image);
                const price = Number(product?.price || item.price || 0);

                return (
                  <div className="checkout-item-row" key={item.productId}>
                    <img
                      src={image || getPlaceholderImage()}
                      alt={product?.title || item.title}
                      className="checkout-item-image"
                    />

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
              <span>{formatCurrency(pricing?.subtotalAmount ?? localSubtotal)}</span>
            </div>

            <div className="summary-row">
              <span>Shipping Fee:</span>
              <span>{formatCurrency(pricing?.shippingFee ?? 0)}</span>
            </div>

            <div className="summary-row">
              <span>Coupon Discount:</span>
              <span>-{formatCurrency(pricing?.discountAmount ?? 0)}</span>
            </div>

            <hr />

            <div className="summary-row total">
              <strong>Order Total:</strong>
              <strong>{formatCurrency(pricing?.totalPrice ?? localSubtotal)}</strong>
            </div>

            <button
              className="btn-place-order"
              onClick={handleCheckout}
              disabled={submitting || pricingLoading || addresses.length === 0 || !selectedAddressId}
            >
              {submitting ? 'Processing...' : 'Place Order'}
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