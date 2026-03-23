import { useState, useEffect } from 'react';
import { getMyAddresses } from '../../api/addressApi';
import { updateOrderAddress } from '../../api/orderApi';
import { useToast } from '../Toast';
import './ChangeAddressModal.css';

const ChangeAddressModal = ({ orderId, currentAddressId, isOpen, onClose, onUpdated }) => {
  const [addresses, setAddresses] = useState([]);
  const [selectedId, setSelectedId] = useState(currentAddressId);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const { showSuccess, showError } = useToast();

  useEffect(() => {
    if (isOpen) {
      loadAddresses();
      setSelectedId(currentAddressId);
    }
  }, [isOpen, currentAddressId]);

  const loadAddresses = async () => {
    try {
      setLoading(true);
      const data = await getMyAddresses();
      setAddresses(data);
    } catch (err) {
      showError('Failed to load your addresses');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!selectedId || selectedId === currentAddressId) {
       onClose();
       return;
    }

    try {
      setSubmitting(true);
      await updateOrderAddress(orderId, selectedId);
      showSuccess('Shipping address updated successfully');
      if (onUpdated) onUpdated();
      onClose();
    } catch (err) {
      showError(err.message || 'Failed to update address');
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay">
      <div className="modal-content address-change-modal">
        <div className="modal-header">
           <h2>Change Shipping Address</h2>
           <button className="close-btn" onClick={onClose}>&times;</button>
        </div>
        
        <div className="modal-body">
           <p className="modal-subtitle">Select a new shipping address below. Note: You can only change the address once.</p>
           
           {loading ? (
             <div className="loading-state">Loading addresses...</div>
           ) : addresses.length === 0 ? (
             <div className="empty-state">No addresses found. Please add a new address in your profile.</div>
           ) : (
             <div className="address-options-list">
                {addresses.map(addr => (
                  <label key={addr.id} className={`address-option-card ${selectedId === addr.id ? 'selected' : ''}`}>
                     <input 
                        type="radio" 
                        name="addressSelect" 
                        value={addr.id} 
                        checked={selectedId === addr.id}
                        onChange={() => setSelectedId(addr.id)}
                     />
                     <div className="address-info">
                        <strong>{addr.fullName}</strong>
                        <p>{addr.street}, {addr.city}</p>
                        <p>{addr.state}, {addr.country}</p>
                     </div>
                  </label>
                ))}
             </div>
           )}
        </div>

        <div className="modal-footer">
           <button className="btn-cancel" onClick={onClose} disabled={submitting}>Cancel</button>
           <button 
             className="btn-save" 
             onClick={handleSave} 
             disabled={submitting || loading || !selectedId}
           >
             {submitting ? 'Updating...' : 'Save & Update Order'}
           </button>
        </div>
      </div>
    </div>
  );
};

export default ChangeAddressModal;
