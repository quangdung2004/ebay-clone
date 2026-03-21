import { useState, useEffect, useCallback } from 'react';
import { getSellerWallet } from '../api/sellerApi';
import { useToast } from '../components/Toast';

export function useSellerWallet() {
  const [wallet, setWallet] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const { showError } = useToast();

  const fetchWallet = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getSellerWallet();
      setWallet(data);
    } catch (err) {
      setError(err);
      showError(err.message || 'Failed to fetch seller wallet');
    } finally {
      setLoading(false);
    }
  }, [showError]);

  useEffect(() => {
    fetchWallet();
  }, [fetchWallet]);

  return { wallet, loading, error, refreshWallet: fetchWallet };
}
