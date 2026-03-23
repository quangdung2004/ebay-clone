import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useSellerWallet } from '../hooks/useSellerWallet';
import { useSellerSettlements } from '../hooks/useSellerSettlements';
import { formatCurrency, formatDateTime } from '../utils/productUtils';
import { SETTLEMENT_STATUS_LABELS, getStatusColorClass } from '../utils/orderStatus';
import './SellerWalletPage.css';

const SellerWalletPage = () => {
  const { wallet, loading: walletLoading, error: walletError } = useSellerWallet();
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const { data: settlementData, loading: settlementsLoading, error: settlementsError } = useSellerSettlements(page, pageSize);

  const totalPages = Math.ceil((settlementData?.total || 0) / pageSize);

  if (walletLoading) {
    return (
      <div className="seller-wallet-page">
        <div className="wallet-loading">Loading your wallet dashboard...</div>
      </div>
    );
  }

  if (walletError || !wallet) {
    return (
      <div className="seller-wallet-page">
        <div className="wallet-error">
          <h3>Failed to load wallet data</h3>
          <p>{walletError?.message || 'Please try again later.'}</p>
        </div>
      </div>
    );
  }

  const handlePageChange = (newPage) => {
    if (newPage > 0 && newPage <= totalPages) {
      setPage(newPage);
    }
  };

  return (
    <div className="seller-wallet-page">
      <div className="wallet-header">
        <h1>Seller Wallet Dashboard</h1>
        <div className="wallet-meta">
          <span>Trust Level: <strong>{wallet.trustLevel}</strong></span>
          {wallet.isVerified && <span className="verified-badge">✓ Verified</span>}
          <span>Last Updated: {formatDateTime(wallet.updatedAt)}</span>
        </div>
      </div>

      <div className="wallet-info-panel">
        <h4>💡 Understanding Your Balance</h4>
        <p>
          <strong>Pending Balance:</strong> payments from buyers currently on hold. 
          <strong> Available Balance:</strong> funds that have passed the hold period and can be utilized or withdrawn. 
          Your <strong>Trust Level</strong> determines the duration of these holds—higher trust means faster access to funds!
        </p>
      </div>

      <div className="wallet-summary-cards">
        <div className="wallet-card warning-card">
          <div className="wallet-card-icon">⏳</div>
          <div className="wallet-card-content">
            <h3>Pending Balance</h3>
            <p className="wallet-card-amount">{formatCurrency(wallet.pendingBalance)}</p>
          </div>
        </div>
        <div className="wallet-card success-card">
          <div className="wallet-card-icon">💸</div>
          <div className="wallet-card-content">
            <h3>Available Balance</h3>
            <p className="wallet-card-amount">{formatCurrency(wallet.availableBalance)}</p>
          </div>
        </div>
        <div className="wallet-card primary-card">
          <div className="wallet-card-icon">📈</div>
          <div className="wallet-card-content">
            <h3>Total Earned</h3>
            <p className="wallet-card-amount">{formatCurrency(wallet.totalEarned)}</p>
          </div>
        </div>
      </div>

      <div className="settlements-section">
        <h2>Settlement History</h2>
        
        {settlementsLoading ? (
          <div className="settlements-loading">Loading settlements...</div>
        ) : settlementsError ? (
          <div className="settlements-error">Failed to load settlements.</div>
        ) : !settlementData?.items?.length ? (
          <div className="settlements-empty">No settlements found.</div>
        ) : (
          <>
            <div className="table-responsive">
              <table className="settlements-table">
                <thead>
                  <tr>
                    <th>Ref ID</th>
                    <th>Product</th>
                    <th>Hold Date</th>
                    <th>Gross</th>
                    <th>Fee</th>
                    <th>Net</th>
                    <th>Status</th>
                    <th>Available At</th>
                  </tr>
                </thead>
                <tbody>
                  {settlementData.items.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <div className="ref-ids">
                          <span title="Settlement ID">S-#{item.id}</span>
                          <span title="Order ID" className="ref-order-id">
                            <Link to={`/orders/${item.orderId}`}>O-#{item.orderId}</Link>
                          </span>
                        </div>
                      </td>
                      <td>
                        <div className="product-title-cell" title={item.productTitle}>
                          {item.productTitle}
                        </div>
                      </td>
                      <td>{formatDateTime(item.heldAt)}</td>
                      <td>{formatCurrency(item.grossAmount)}</td>
                      <td className="fee-cell">-{formatCurrency(item.platformFee)}</td>
                      <td className="net-amount">{formatCurrency(item.netAmount)}</td>
                      <td>
                        <span className={`status-badge ${getStatusColorClass(item.status)}`}>
                          {SETTLEMENT_STATUS_LABELS[item.status] || item.status}
                        </span>
                      </td>
                      <td>{formatDateTime(item.availableAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <div className="pagination">
                <button 
                  onClick={() => handlePageChange(page - 1)} 
                  disabled={page === 1}
                  className="btn-page"
                >
                  Previous
                </button>
                <span className="page-info">
                  Page {page} of {totalPages}
                </span>
                <button 
                  onClick={() => handlePageChange(page + 1)} 
                  disabled={page === totalPages}
                  className="btn-page"
                >
                  Next
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default SellerWalletPage;
