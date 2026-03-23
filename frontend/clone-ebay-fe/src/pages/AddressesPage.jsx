import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { createAddress, getMyAddresses, updateAddress } from '../api/addressApi';
import { useToast } from '../components/Toast';
import { parseApiError } from '../utils/errorUtils';
import {
  getSelectedShippingAddressId,
  setSelectedShippingAddressId,
} from '../utils/checkoutAddress';
import { formatCoordinates } from '../utils/shippingMap';
import './AddressesPage.css';

const emptyForm = {
  fullName: '',
  phone: '',
  street: '',
  city: '',
  state: '',
  country: 'Vietnam',
  latitude: '',
  longitude: '',
  isDefault: true,
};

const AddressesPage = () => {
  const { search } = useLocation();
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();

  const redirect = useMemo(
    () => new URLSearchParams(search).get('redirect') || '',
    [search]
  );

  const [addresses, setAddresses] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [selectedAddressId, setSelectedAddressId] = useState(
    getSelectedShippingAddressId()
  );
  const [saving, setSaving] = useState(false);

  const loadAddresses = async () => {
    try {
      const data = await getMyAddresses();
      setAddresses(data);

      if (data.length) {
        const selected =
          data.find((item) => item.id === getSelectedShippingAddressId()) ||
          data.find((item) => item.isDefault) ||
          data[0];

        setSelectedAddressId(selected.id);
        setSelectedShippingAddressId(selected.id);
      }
    } catch (error) {
      showError(parseApiError(error, 'Failed to load addresses').message);
    }
  };

  useEffect(() => {
    loadAddresses();
  }, []);

const toNullableNumber = (value) => {
  const text = String(value ?? '').trim();
  if (!text) return null;

  const parsed = Number(text);
  return Number.isNaN(parsed) ? NaN : parsed;
};

const onSubmit = async (e) => {
  e.preventDefault();

  const latitude = toNullableNumber(form.latitude);
  const longitude = toNullableNumber(form.longitude);

  if (Number.isNaN(latitude) || Number.isNaN(longitude)) {
    showError('Invalid latitude/longitude.');
    return;
  }

  const payload = {
    ...form,
    latitude,
    longitude,
  };

    try {
      setSaving(true);

      let savedAddress;

      if (editingId) {
        savedAddress = await updateAddress(editingId, payload);
        showSuccess('Address updated successfully.');
      } else {
        savedAddress = await createAddress(payload);
        showSuccess('Address created successfully.');
      }

      if (savedAddress?.id) {
        setSelectedAddressId(savedAddress.id);
        setSelectedShippingAddressId(savedAddress.id);
      }

      setEditingId(null);
      setForm(emptyForm);
      await loadAddresses();
    } catch (error) {
      showError(parseApiError(error, 'Failed to save address').message);
    } finally {
      setSaving(false);
    }
  };

  const handleSelectAddress = (addressId) => {
    setSelectedAddressId(addressId);
    setSelectedShippingAddressId(addressId);
  };

  const handleUseSelectedAddress = async () => {
  if (!selectedAddressId) {
    showError('Please select a shipping address.');
    return;
  }

  const selected = addresses.find((item) => item.id === selectedAddressId);
  if (!selected) {
    showError('Selected address not found.');
    return;
  }

  try {
    setSaving(true);

    await updateAddress(selected.id, {
      fullName: selected.fullName || '',
      phone: selected.phone || '',
      street: selected.street || '',
      city: selected.city || '',
      state: selected.state || '',
      country: selected.country || 'Vietnam',
      latitude: selected.latitude ?? null,
      longitude: selected.longitude ?? null,
      isDefault: true,
    });

    setSelectedShippingAddressId(selected.id);
    showSuccess('Selected address is now the default address.');

    await loadAddresses();

    if (redirect) {
      navigate(redirect);
      return;
    }

    navigate('/cart');
  } catch (error) {
    showError(parseApiError(error, 'Failed to use selected address').message);
  } finally {
    setSaving(false);
  }


    setSelectedShippingAddressId(selectedAddressId);
    showSuccess('Shipping address selected.');

    if (redirect) {
      navigate(redirect);
      return;
    }

    navigate('/cart');
  };

  return (
    <div className="address-page">
      <div className="address-layout">
        <section className="address-list-card">
          <div className="address-header">
            <h1>Your addresses</h1>
            <button
              className="address-add-btn"
              onClick={() => {
                setEditingId(null);
                setForm(emptyForm);
              }}
            >
              + Add address
            </button>
          </div>

          {!addresses.length ? (
            <p className="address-empty">
              You do not have any addresses yet. Create one to place an order.
            </p>
          ) : (
            <>
              {addresses.map((item) => (
                <div
                  key={item.id}
                  className={`address-item ${selectedAddressId === item.id ? 'active' : ''}`}
                >
                  <div className="address-item-top">
                    <label className="address-select-label">
                      <input
                        type="radio"
                        name="shippingAddress"
                        checked={selectedAddressId === item.id}
                        onChange={() => handleSelectAddress(item.id)}
                      />
                      <span>Select as shipping address</span>
                    </label>

                    <button
                      type="button"
                      className="address-edit-btn"
                      onClick={() => {
                        setEditingId(item.id);
                        setForm({
                          fullName: item.fullName || '',
                          phone: item.phone || '',
                          street: item.street || '',
                          city: item.city || '',
                          state: item.state || '',
                          country: item.country || 'Vietnam',
                          latitude: item.latitude ?? '',
                          longitude: item.longitude ?? '',
                          isDefault: !!item.isDefault,
                        });
                      }}
                    >
                      Edit
                    </button>
                  </div>

                  <div className="address-name-row">
                    <strong>{item.fullName}</strong>
                    {item.isDefault && <span className="address-badge">Default</span>}
                  </div>

                  <div>{item.phone}</div>
                  <div>{item.street}</div>
                  <div>{item.state}, {item.city}, {item.country}</div>
                  <div>
                    Coordinates:{' '}
                    {(item.latitude != null && item.longitude != null)
                      ? formatCoordinates(item.latitude, item.longitude)
                      : '--'}
                  </div>
                </div>
              ))}

              <button
                type="button"
                className="address-use-btn"
                onClick={handleUseSelectedAddress}
              >
                Use selected address
              </button>
            </>
          )}
        </section>

        <section className="address-form-card">
          <h2>{editingId ? 'Edit address' : 'Add address'}</h2>

          <form onSubmit={onSubmit} className="address-form">
            <input
              value={form.fullName}
              placeholder="Full name"
              onChange={(e) => setForm((prev) => ({ ...prev, fullName: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.phone}
              placeholder="Phone number"
              onChange={(e) => setForm((prev) => ({ ...prev, phone: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.street}
              placeholder="Street address"
              onChange={(e) => setForm((prev) => ({ ...prev, street: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.city}
              placeholder="City / District"
              onChange={(e) => setForm((prev) => ({ ...prev, city: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.state}
              placeholder="State / Province"
              onChange={(e) => setForm((prev) => ({ ...prev, state: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.country}
              placeholder="Country"
              onChange={(e) => setForm((prev) => ({ ...prev, country: e.target.value }))}
              className="address-input"
              required
            />

            <input
              type="number"
              step="0.0000001"
              value={form.latitude}
              placeholder="Latitude (e.g. 10.776889) Optional"
              onChange={(e) => setForm((prev) => ({ ...prev, latitude: e.target.value }))}
              className="address-input"
            />

            <input
              type="number"
              step="0.0000001"
              value={form.longitude}
              placeholder="Longitude (e.g. 106.700806) Optional"
              onChange={(e) => setForm((prev) => ({ ...prev, longitude: e.target.value }))}
              className="address-input"

            />

            <label className="address-checkbox">
              <input
                type="checkbox"
                checked={!!form.isDefault}
                onChange={(e) => setForm((prev) => ({ ...prev, isDefault: e.target.checked }))}
              />
              Set as default address
            </label>

            <button className="address-save-btn" type="submit" disabled={saving}>
              {saving ? 'Saving...' : editingId ? 'Update address' : 'Create address'}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
};

export default AddressesPage;
