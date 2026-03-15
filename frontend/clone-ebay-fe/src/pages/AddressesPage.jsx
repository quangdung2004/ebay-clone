import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { createAddress, getMyAddresses, updateAddress } from '../api/addressApi';
import { useToast } from '../components/Toast';
import { parseApiError } from '../utils/errorUtils';
import {
  getSelectedShippingAddressId,
  setSelectedShippingAddressId,
} from '../utils/checkoutAddress';
import './AddressesPage.css';

const emptyForm = {
  fullName: '',
  phone: '',
  street: '',
  city: '',
  state: '',
  country: 'Vietnam',
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

  const onSubmit = async (e) => {
    e.preventDefault();
    try {
      setSaving(true);

      let savedAddress;

      if (editingId) {
        savedAddress = await updateAddress(editingId, form);
        showSuccess('Address updated successfully.');
      } else {
        savedAddress = await createAddress(form);
        showSuccess('Address created successfully.');
      }

      if (savedAddress?.id) {
        setSelectedAddressId(savedAddress.id);
        setSelectedShippingAddressId(savedAddress.id);
      }

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

  const handleUseSelectedAddress = () => {
    if (!selectedAddressId) {
      showError('Please select a shipping address.');
      return;
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
            <h1>Địa chỉ của bạn</h1>
            <button
              className="address-add-btn"
              onClick={() => {
                setEditingId(null);
                setForm(emptyForm);
              }}
            >
              + Thêm địa chỉ
            </button>
          </div>

          {!addresses.length ? (
            <p className="address-empty">
              Bạn chưa có địa chỉ nào. Hãy tạo địa chỉ để đặt hàng.
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
                      <span>Chọn làm địa chỉ giao hàng</span>
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
                          isDefault: !!item.isDefault,
                        });
                      }}
                    >
                      Chỉnh sửa
                    </button>
                  </div>

                  <div className="address-name-row">
                    <strong>{item.fullName}</strong>
                    {item.isDefault && <span className="address-badge">Mặc định</span>}
                  </div>

                  <div>{item.phone}</div>
                  <div>{item.street}</div>
                  <div>{item.state}, {item.city}, {item.country}</div>
                </div>
              ))}

              <button
                type="button"
                className="address-use-btn"
                onClick={handleUseSelectedAddress}
              >
                Dùng địa chỉ đã chọn
              </button>
            </>
          )}
        </section>

        <section className="address-form-card">
          <h2>{editingId ? 'Chỉnh sửa địa chỉ' : 'Thêm địa chỉ'}</h2>

          <form onSubmit={onSubmit} className="address-form">
            <input
              value={form.fullName}
              placeholder="Họ và tên"
              onChange={(e) => setForm((prev) => ({ ...prev, fullName: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.phone}
              placeholder="Số điện thoại"
              onChange={(e) => setForm((prev) => ({ ...prev, phone: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.street}
              placeholder="Số nhà, đường"
              onChange={(e) => setForm((prev) => ({ ...prev, street: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.city}
              placeholder="Thành phố / Quận huyện"
              onChange={(e) => setForm((prev) => ({ ...prev, city: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.state}
              placeholder="Tỉnh / Bang"
              onChange={(e) => setForm((prev) => ({ ...prev, state: e.target.value }))}
              className="address-input"
              required
            />

            <input
              value={form.country}
              placeholder="Quốc gia"
              onChange={(e) => setForm((prev) => ({ ...prev, country: e.target.value }))}
              className="address-input"
              required
            />

            <label className="address-checkbox">
              <input
                type="checkbox"
                checked={!!form.isDefault}
                onChange={(e) => setForm((prev) => ({ ...prev, isDefault: e.target.checked }))}
              />
              Đặt làm địa chỉ mặc định
            </label>

            <button className="address-save-btn" type="submit" disabled={saving}>
              {saving ? 'Đang lưu...' : editingId ? 'Cập nhật địa chỉ' : 'Tạo địa chỉ'}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
};

export default AddressesPage;
