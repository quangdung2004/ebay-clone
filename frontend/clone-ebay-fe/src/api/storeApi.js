import axiosInstance from './axios';

/**
 * Get the current user's store.
 * @returns {Promise<Object>} StoreDto
 */
export const getMyStore = async () => {
    const response = await axiosInstance.get('/stores/me');
    return response.data.data;
};

/**
 * Create a new store for the current user.
 * @param {{ storeName: string, description?: string, bannerImageURL?: string|null }} payload
 * @returns {Promise<Object>} StoreDto
 */
export const createStore = async (payload) => {
    const response = await axiosInstance.post('/stores', payload);
    return response.data.data;
};

/**
 * Update the current user's store.
 * @param {{ storeName?: string, description?: string, bannerImageURL?: string|null }} payload
 * @returns {Promise<Object>} StoreDto
 */
export const updateMyStore = async (payload) => {
    const response = await axiosInstance.put('/stores/me', payload);
    return response.data.data;
};
