import { useState } from 'react';
import { cancelOrder } from '../../api/orderApi';
import { markOrderProcessing, shipOrder } from '../../api/sellerApi';
import { useToast } from '../Toast';


const OrderActionBar = ({ order, isSeller, isBuyer, onOrderUpdated }) => {
  const [isActionLoading, setIsActionLoading] = useState(false);
  const [shippingForm, setShippingForm] = useState({
    carrier: '',
    trackingNumber: '',
    estimatedArrival: '',
    provider: '17TRACK'
  });
  const { showSuccess, showError } = useToast();

  if (!order) return null;

  const validToCancel = isBuyer && order.status === 'PENDING_PAYMENT';
  
  const latestPayment = order.payments && order.payments.length > 0
    ? order.payments[order.payments.length - 1]
    : {};

  const handleCancelClick = async () => {
    const isConfirmed = window.confirm('Are you sure you want to cancel this order?');
    if (!isConfirmed) return;

    try {
      setIsActionLoading(true);
      await cancelOrder(order.id);
      showSuccess('Order has been cancelled successfully');
      if (onOrderUpdated) onOrderUpdated();
    } catch (err) {
      showError(err.message || 'Failed to cancel the order');
    } finally {
      setIsActionLoading(false);
    }
  };

  const handleMarkProcessing = async () => {
    try {
      setIsActionLoading(true);
      await markOrderProcessing(order.id);
      showSuccess('Order marked as processing');
      if (onOrderUpdated) onOrderUpdated();
    } catch (err) {
      showError(err.message || 'Failed to update order status');
    } finally {
      setIsActionLoading(false);
    }
  };

  const handleShipOrder = async (e) => {
    e.preventDefault();
    if (!shippingForm.carrier || !shippingForm.trackingNumber) {
      showError('Carrier and Tracking Number are required.');
      return;
    }
    try {
        setIsActionLoading(true);
        let arrivalDate = null;
        if (shippingForm.estimatedArrival) {
           const d = new Date(shippingForm.estimatedArrival);
           // Add time part so it's a valid timestamp (e.g. end of day)
           d.setHours(23, 59, 59);
           arrivalDate = d.toISOString();
        }

        await shipOrder(order.id, {
            carrier: shippingForm.carrier,
            trackingNumber: shippingForm.trackingNumber,
            estimatedArrival: arrivalDate,
            provider: shippingForm.provider
        });
        showSuccess('Order has been shipped successfully');
        if (onOrderUpdated) onOrderUpdated();
    } catch (err) {
        showError(err.message || 'Failed to ship the order');
    } finally {
        setIsActionLoading(false);
    }
  };

  return (
    <div className="order-action-bar">
       {/* General messages for Buyer */}
       {isBuyer && (order.status === 'PAID' || latestPayment?.status === 'CAPTURED') && (
         <div className="order-message-success" style={{ padding: '12px', background: '#e5f4e3', color: '#105c08', borderRadius: '4px', marginBottom: '15px' }}>
           This order has been successfully paid.
         </div>
       )}
    
       {order.status === 'CANCELLED' && (
         <div className="order-message-error" style={{ padding: '12px', background: '#fcebe8', color: '#a0050b', borderRadius: '4px', marginBottom: '15px' }}>
           This order is cancelled.
         </div>
       )}

       {/* Actions */}
       <div className="order-action-buttons">
         {validToCancel && (
           <button
             type="button"
             className="btn-order-cancel"
             onClick={handleCancelClick}
             disabled={isActionLoading}
             style={{ padding: '0.75rem 1.5rem', background: '#fff', color: '#cc0000', border: '1px solid #cc0000', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}
           >
             {isActionLoading ? 'Cancelling...' : 'Cancel Order'}
           </button>
         )}

         {isSeller && String(order?.status).toUpperCase() === 'PAID' && (
           <button
             type="button"
             className="btn-order-processing"
             onClick={handleMarkProcessing}
             disabled={isActionLoading}
             style={{ padding: '0.75rem 1.5rem', background: '#0053a0', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}
           >
             {isActionLoading ? 'Processing...' : 'Mark Processing'}
           </button>
         )}
       </div>

       {isSeller && String(order?.status).toUpperCase() === 'PROCESSING' && (
         <div className="order-shipping-form" style={{ marginTop: '20px', padding: '20px', background: '#f8f8f8', border: '1px solid #ddd', borderRadius: '8px' }}>
           <h3 style={{ margin: '0 0 15px 0' }}>Ship Order</h3>
           <form onSubmit={handleShipOrder} style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
             <div>
               <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>Carrier *</label>
               <input 
                 type="text" 
                 value={shippingForm.carrier} 
                 onChange={e => setShippingForm({...shippingForm, carrier: e.target.value})} 
                 placeholder="e.g. FedEx, UPS, USPS"
                 style={{ width: '100%', padding: '10px', border: '1px solid #ccc', borderRadius: '4px' }}
                 required
               />
             </div>
             <div>
               <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>Tracking Number *</label>
               <input 
                 type="text" 
                 value={shippingForm.trackingNumber} 
                 onChange={e => setShippingForm({...shippingForm, trackingNumber: e.target.value})} 
                 placeholder="Tracking Number"
                 style={{ width: '100%', padding: '10px', border: '1px solid #ccc', borderRadius: '4px' }}
                 required
               />
             </div>
             <div>
               <label style={{ display: 'block', marginBottom: '5px', fontWeight: '500' }}>Estimated Arrival</label>
               <input 
                 type="date" 
                 value={shippingForm.estimatedArrival} 
                 onChange={e => setShippingForm({...shippingForm, estimatedArrival: e.target.value})} 
                 style={{ width: '100%', padding: '10px', border: '1px solid #ccc', borderRadius: '4px' }}
               />
             </div>
             <div>
               <button 
                 type="submit" 
                 disabled={isActionLoading}
                 style={{ padding: '0.75rem 1.5rem', background: '#0053a0', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold', width: '100%' }}
               >
                 {isActionLoading ? 'Submitting...' : 'Ship Order'}
               </button>
             </div>
             </form>
         </div>
       )}

       {/* Simulate Delivery (Mock Status) Group for Seller / Tester */}
       {import.meta.env.DEV && isSeller && String(order?.status).toUpperCase() === 'SHIPPED' && (
         <div style={{ marginTop: '20px', padding: '15px', background: '#f0f4f8', border: '1px dashed #0053a0', borderRadius: '8px' }}>
            <h4 style={{ margin: '0 0 10px 0', fontSize: '0.9rem', color: '#0053a0' }}>🛠 DEV Simulation Console</h4>
            <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
               <button
                 type="button"
                 onClick={async () => {
                    try {
                      setIsActionLoading(true);
                      const { applyMockShipmentStatus } = await import('../../api/sellerApi');
                      await applyMockShipmentStatus(order.id, {
                        status: 'IN_TRANSIT',
                        description: 'Shipment departed from Los Angeles hub',
                        location: 'Los Angeles, California, USA',
                        eventTime: new Date().toISOString()
                      });
                      showSuccess('Mock: In Transit set');
                      if (onOrderUpdated) onOrderUpdated();
                    } catch(e) { showError(e.message); } finally { setIsActionLoading(false); }
                 }}
                 disabled={isActionLoading}
                 style={{ padding: '0.5rem 0.75rem', background: '#fff', border: '1px solid #ccc', borderRadius: '4px', cursor: 'pointer', fontSize: '0.85rem' }}
               >
                 1. In Transit
               </button>

               <button
                 type="button"
                 onClick={async () => {
                    try {
                      setIsActionLoading(true);
                      const { applyMockShipmentStatus } = await import('../../api/sellerApi');
                      await applyMockShipmentStatus(order.id, {
                        status: 'OUT_FOR_DELIVERY',
                        description: 'Package out for delivery in Chicago',
                        location: 'Chicago, Illinois, USA',
                        eventTime: new Date().toISOString()
                      });
                      showSuccess('Mock: Out for Delivery set');
                      if (onOrderUpdated) onOrderUpdated();
                    } catch(e) { showError(e.message); } finally { setIsActionLoading(false); }
                 }}
                 disabled={isActionLoading}
                 style={{ padding: '0.5rem 0.75rem', background: '#fff', border: '1px solid #ccc', borderRadius: '4px', cursor: 'pointer', fontSize: '0.85rem' }}
               >
                 2. Out for Delivery
               </button>

               <button
                 type="button"
                 onClick={async () => {
                    try {
                      setIsActionLoading(true);
                      const { applyMockShipmentStatus } = await import('../../api/sellerApi');
                      await applyMockShipmentStatus(order.id, {
                        status: 'DELIVERED',
                        description: 'Package delivered to recipient in New York',
                        location: 'New York City, New York, USA',
                        eventTime: new Date().toISOString()
                      });
                      showSuccess('Mock: Delivered set');
                      if (onOrderUpdated) onOrderUpdated();
                    } catch(e) { showError(e.message); } finally { setIsActionLoading(false); }
                 }}
                 disabled={isActionLoading}
                 style={{ padding: '0.5rem 0.75rem', background: '#0053a0', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '0.85rem', fontWeight: 'bold' }}
               >
                 3. Delivered (Finish)
               </button>
            </div>
         </div>
       )}
    </div>
  );
};

export default OrderActionBar;
