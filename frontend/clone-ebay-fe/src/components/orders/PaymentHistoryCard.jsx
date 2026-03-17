import { PAYMENT_METHOD_LABELS, PAYMENT_STATUS_LABELS } from '../../utils/orderStatus';

function formatDate(dateString) {
  if (!dateString) return '--';
  return new Date(dateString).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

function formatCurrency(value) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value || 0);
}

const PaymentHistoryCard = ({ payments }) => {
  if (!payments || payments.length === 0) {
    return (
      <div className="order-card-panel">
        <h3 className="order-section-title">Payment History</h3>
        <p className="order-text-muted">No payment history available.</p>
      </div>
    );
  }

  return (
    <div className="order-card-panel">
      <h3 className="order-section-title">Payment History</h3>
      <div className="payment-history-list">
        {payments.map((p) => (
          <div className="payment-history-item" key={p.id}>
            <div className="payment-history-row">
              <span className="payment-history-label">Method:</span>
              <strong className="payment-history-value">
                {PAYMENT_METHOD_LABELS[p.method] || p.method}
              </strong>
            </div>
            <div className="payment-history-row">
              <span className="payment-history-label">Status:</span>
              <strong className="payment-history-value">
                 {PAYMENT_STATUS_LABELS[p.status] || p.status}
              </strong>
            </div>
            <div className="payment-history-row">
              <span className="payment-history-label">Amount:</span>
              <strong className="payment-history-value">{formatCurrency(p.amount)}</strong>
            </div>
            {p.paidAt && (
              <div className="payment-history-row">
                <span className="payment-history-label">Paid At:</span>
                <span className="payment-history-value">{formatDate(p.paidAt)}</span>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

export default PaymentHistoryCard;
