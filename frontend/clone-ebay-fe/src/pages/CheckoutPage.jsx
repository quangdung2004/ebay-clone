import { useEffect, useState, useMemo } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { getMyAddresses } from '../api/addressApi';
import { checkoutOrder } from '../api/orderApi';
import { useToast } from '../components/Toast';
import { formatCurrency, getPlaceholderImage, normalizeProductImageUrl } from '../utils/productUtils';
import { removeFromCart } from '../utils/cartUtils';
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

  useEffect(() => {
    // Read items from location state
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

  const total = useMemo(() => {
    return items.reduce((sum, item) => {
      return sum + Number(item.product?.price || item.price || 0) * Number(item.quantity || 0);
    }, 0);
  }, [items]);

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
        items: items.map(i => ({
          productId: Number(i.productId),
          quantity: Number(i.quantity)
        }))
      };

      const result = await checkoutOrder(payload);

      // Remove checked out items from cart
      items.forEach(i => removeFromCart(i.productId));

      showSuccess('Order created successfully!');
      
      // result should ideally have order ids. In the original logic, it might return a list of orders (if multiple sellers)
      // We will navigate to the generic my orders list, or /orders/:id if only one.
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
              {addresses.map(addr => (
                <label key={addr.id} className={`checkout-address-card ${selectedAddressId === addr.id ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="address"
                    value={addr.id}
                    checked={selectedAddressId === addr.id}
                    onChange={(e) => {
                      setSelectedAddressId(addr.id);
                      storeSelectedAddressId(addr.id);
                    }}
                  />
                  <div className="checkout-address-content">
                    <strong>{addr.fullName} <span className="checkout-address-phone">{addr.phone}</span></strong>
                    <p>{addr.street}, {addr.city}, {addr.state}, {addr.country}</p>
                    {addr.isDefault && <span className="badge-default-address">Default</span>}
                  </div>
                </label>
              ))}
              {addresses.length === 0 && (
                <div className="checkout-no-address">You don't have any shipping address. Please add one to continue.</div>
              )}
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
               {items.map(item => {
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
              <span>Items ({items.reduce((s,i) => s + Number(i.quantity), 0)}):</span>
              <span>{formatCurrency(total)}</span>
            </div>
            <div className="summary-row">
              <span>Shipping:</span>
              <span>Free</span>
            </div>
            <hr />
            <div className="summary-row total">
              <strong>Order Total:</strong>
              <strong>{formatCurrency(total)}</strong>
            </div>

            <button
              className="btn-place-order"
              onClick={handleCheckout}
              disabled={submitting || addresses.length === 0 || !selectedAddressId}
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
