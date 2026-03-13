import axiosInstance from './axios';

/**
 * @param {{q?: string, categoryId?: number, minPrice?: number, maxPrice?: number, sort?: string, page?: number, pageSize?: number}} params
 * @returns {Promise<{items: Array, page: number, pageSize: number, total: number}>}
 */
export const getProducts = async (params = {}) => {
    // Strip undefined/empty values so axios doesn't send them
    const clean = {};
    for (const [k, v] of Object.entries(params)) {
        if (v !== undefined && v !== null && v !== '') clean[k] = v;
    }
    const response = await axiosInstance.get('/products', { params: clean });
    return response.data.data; // PagedResponse<ProductListItemDto>
};

/**
 * @param {number|string} id
 * @returns {Promise<Object>} ProductDetailDto
 */
export const getProductById = async (id) => {
    const response = await axiosInstance.get(`/products/${id}`);
    return response.data.data;
};

/**
 * @param {{q?: string, categoryId?: number, minPrice?: number, maxPrice?: number, sort?: string, status?: string, condition?: string, isAuction?: boolean, inStock?: boolean, page?: number, pageSize?: number}} params
 * @returns {Promise<{items: Array, page: number, pageSize: number, total: number}>}
 */
export const getMyProducts = async (params = {}) => {
    const clean = {};
    for (const [k, v] of Object.entries(params)) {
        if (v !== undefined && v !== null && v !== '') clean[k] = v;
    }
    const response = await axiosInstance.get('/products/my', { params: clean });
    return response.data.data;
};

/**
 * @param {{title: string, description?: string, price: number, categoryId?: number, isAuction?: boolean, auctionEndTime?: string, quantity: number}} body
 * @returns {Promise<Object>} ProductDetailDto
 */
export const createProduct = async (body) => {
    const response = await axiosInstance.post('/products', body);
    return response.data.data;
};

/**
 * @param {number|string} id
 * @param {{title?: string, description?: string, price?: number, categoryId?: number, isAuction?: boolean, auctionEndTime?: string}} body
 * @returns {Promise<Object>} ProductDetailDto
 */
export const updateProduct = async (id, body) => {
    const response = await axiosInstance.put(`/products/${id}`, body);
    return response.data.data;
};

/**
 * @param {number|string} id
 * @param {{quantity: number}} body
 */
export const updateInventory = async (id, body) => {
    const response = await axiosInstance.put(`/products/${id}/inventory`, body);
    return response.data?.data ?? null;
};

/**
 * @param {number|string} id
 * @param {{status: string}} body
 */
export const updateProductStatus = async (id, body) => {
    const response = await axiosInstance.patch(`/products/${id}/status`, body);
    return response.data?.data ?? null;
};

/**
 * @param {number|string} id
 * @param {File[]} files
 * @returns {Promise<Object>} ProductDetailDto with updated images
 */
export const uploadProductImages = async (id, files) => {
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));
    const response = await axiosInstance.post(`/products/${id}/images`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data.data;
};

/**
 * @param {number|string} id
 * @param {string} imageUrl
 */
export const deleteProductImage = async (id, imageUrl) => {
    const response = await axiosInstance.delete(`/products/${id}/images`, {
        data: { imageUrl }
    });
    return response.data?.data ?? null;
};

/**
 * @param {number|string} id
 */
export const deleteProduct = async (id) => {
    const response = await axiosInstance.delete(`/products/${id}`);
    return response.data?.data ?? null;
};
